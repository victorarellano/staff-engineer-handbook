# 07 — Distributed Inventory

## Problem

How do we keep inventory consistent when multiple concurrent operations — including multiple application instances — modify the same stock?

This exercise starts with an in-memory race condition and progressively moves coordination to PostgreSQL. The goal is not to identify one universally correct solution, but to understand what guarantee each strategy provides and where that strategy stops being sufficient.

## Invariant

```text
AvailableStock >= 0
```

The final stock value alone is not enough to prove correctness. The number of accepted purchases must also be consistent with the available inventory. A seemingly valid final stock can still hide overselling or a lost update.

---

## 1. Naive Inventory Update

The initial operation separates the logical steps:

```text
READ stock
CHECK stock >= quantity
DELAY
UPDATE stock
```

Two concurrent operations can read the same state before either one updates it. With an initial stock of `1`, both can pass the check.

In the in-memory implementation, the race produced negative stock. Depending on the update strategy, the same race can also appear as a **lost update**: two successful purchases with a final stock that still appears valid.

**Lesson:** `READ → CHECK → UPDATE` is not atomic.

---

## 2. Local Lock

The complete critical section is protected:

```text
lock
 └─ READ → CHECK → UPDATE
```

When both operations share the same synchronization object, only one can evaluate and modify the inventory at a time.

With an initial stock of `1`, one purchase succeeds, the other is rejected, and the final stock is `0`.

**Limitation:** the lock exists only inside one process.

---

## 3. Multiple Application Instances

Two independent application instances have independent local locks:

```text
Instance A → local lock A
Instance B → local lock B

lock A != lock B
```

Even when both instances access the same repository, their local locks do not coordinate with each other.

Observed behavior:

```text
Instance A reads stock 1
Instance B reads stock 1
Instance A completed purchase
Instance B completed purchase

Successful purchases: 2
Final stock: 0
```

The final stock looks valid, but two units were sold when only one existed.

This is **overselling combined with a lost update**.

**Lesson:** a local lock cannot coordinate separate processes, pods, or servers.

---

## 4. Shared Resource Synchronization

Synchronization is moved to the shared in-memory repository. The repository protects the logical operation:

```text
CHECK + UPDATE
```

The result becomes consistent again: one purchase succeeds and the other is rejected.

**Limitation:** the repository is still a singleton inside the same process. This scenario demonstrates that coordination must move to the shared resource, but it is not yet a distributed solution.

---

## 5. Atomic Database Update

Coordination is moved to PostgreSQL:

```sql
UPDATE product_inventory
SET available_stock = available_stock - @quantity
WHERE product_id = @productId
  AND available_stock >= @quantity;
```

The business condition and the state change are expressed in a single database statement.

```text
1 affected row → purchase accepted
0 affected rows → purchase rejected
```

The database also protects the invariant with:

```sql
CHECK (available_stock >= 0)
```

Different application instances can now coordinate through a resource they actually share.

**Lesson:** for a simple stock decrement, a conditional atomic `UPDATE` provides a strong solution with little application-side complexity.

---

## 6. Optimistic Concurrency

A `version` column is introduced. The operation deliberately separates reading from writing:

```text
READ stock + version
        ↓
business logic
        ↓
UPDATE WHERE version = expectedVersion
```

The update becomes:

```sql
UPDATE product_inventory
SET available_stock = @newStock,
    version = version + 1
WHERE product_id = @productId
  AND version = @expectedVersion;
```

If two instances read:

```text
stock = 1
version = 1
```

only the first update can succeed. It changes the version to `2`. The second update affects zero rows because its expected version is stale.

```text
A → update succeeds → version 2
B → update fails    → concurrency conflict
```

**Lesson:** a concurrency conflict does not necessarily mean insufficient stock. It means the state used to make the decision changed after it was read.

---

## 7. Optimistic Concurrency + Retry

After a concurrency conflict, the operation must not blindly repeat the stale update.

```text
conflict
   ↓
READ current state again
   ↓
RE-EVALUATE business rule
   ↓
retry if still valid
```

With initial stock `1`:

```text
A succeeds
B conflicts
B reads stock 0
B is rejected
```

With initial stock `2`:

```text
A succeeds → stock 1, version 2
B conflicts
B reads stock 1, version 2
B retries successfully → stock 0, version 3
```

This separates two different outcomes:

```text
Concurrency conflict → previously read state became stale
Business rejection   → current state does not allow the operation
```

**Lesson:** retry means re-reading current state and re-evaluating the business rule.

---

## 8. Distributed Lock — PostgreSQL Advisory Lock

Another strategy is to prevent multiple application instances from entering the critical section simultaneously.

The exercise uses a PostgreSQL session-level advisory lock:

```sql
SELECT pg_advisory_lock(@lockKey);
```

For the lab, `productId` is used as the lock key.

```text
Instance A
    ↓
Acquire lock
    ↓
READ → CHECK → UPDATE
    ↓
Release lock

Instance B
    ↓
Wait for the same lock
```

The lock is released with:

```sql
SELECT pg_advisory_unlock(@lockKey);
```

Because this is a session-level advisory lock, acquisition and release must occur on the same PostgreSQL session.

The release is protected with `try/finally` and is attempted only when the lock was actually acquired.

### Optimistic Concurrency vs. Distributed Lock

```text
Optimistic Concurrency
→ allow concurrent work
→ detect stale state at write time

Distributed Lock
→ coordinate before entering
→ serialize the critical section
```

---

## 9. Distributed Lock + Timeout

`pg_advisory_lock()` can wait indefinitely. To bound that waiting time, the exercise uses:

```sql
SELECT pg_try_advisory_lock(@lockKey);
```

It returns immediately:

```text
true  → lock acquired
false → lock busy
```

The application retries until the configured timeout is reached.

```text
Instance A → acquires lock
             holds critical section

Instance B → try
             retry
             retry
             timeout
```

### Implementation bug found during the exercise

The first implementation called:

```csharp
await TryAcquireLockAsync(...);
```

but ignored its returned `bool`. The critical section therefore continued even when the lock had not been acquired.

The correction was:

```csharp
lockAcquired = await TryAcquireLockAsync(...);

if (!lockAcquired)
{
    return false;
}
```

This also exposes an important semantic distinction:

```text
Not enough stock → business rejection
Lock timeout      → coordination failure
```

---

## 10. Distributed Lock Failure Recovery

The next scenario simulates an exception while an instance owns the lock.

```text
Acquire
   ↓
Critical section
   ↓
EXCEPTION
   ↓
catch
   ↓
finally
   ↓
Release
```

The experiment confirmed that the second instance can continue after the first instance releases the lock in `finally`.

The cleanup uses:

```csharp
CancellationToken.None
```

when releasing the already-acquired resource. Reusing an already-cancelled operation token could prevent the cleanup attempt itself.

**Lesson:** acquiring a resource creates a cleanup responsibility, even when the protected operation fails.

---

## 11. Distributed Lock Session Failure

The final scenario explores what happens when a session-level advisory lock is not explicitly released with:

```sql
pg_advisory_unlock(...)
```

The tested sequence is:

```text
Connection A
    ↓
pg_advisory_lock
    ↓
no explicit unlock
    ↓
PostgreSQL session ends
    ↓
lock is released
    ↓
Connection B acquires the same lock
```

The experiment demonstrates the lifecycle of the PostgreSQL session-level advisory lock used by the lab: the lock belongs to the owning database session and does not remain permanently held after that session ends.

This scenario does not model every possible distributed failure. It isolates the behavior we wanted to verify: **what happens to a session advisory lock when its owning PostgreSQL session disappears**.

---

# Database Evolution

`AtomicDatabaseUpdate` keeps the original schema in:

```text
init.sql
```

To preserve that scenario, optimistic concurrency evolves the database through a separate script:

```text
02-add-optimistic-concurrency.sql
```

```sql
ALTER TABLE product_inventory
ADD COLUMN IF NOT EXISTS version INTEGER NOT NULL DEFAULT 1;
```

## Apply the schema evolution

From `DistributedInventorySimulation`:

```powershell
Get-Content .\02-add-optimistic-concurrency.sql |
docker compose exec -T postgres psql `
  -U inventory_user `
  -d distributed_inventory
```

Verify:

```powershell
docker compose exec postgres psql `
  -U inventory_user `
  -d distributed_inventory `
  -c "\d product_inventory"
```

The table should contain:

```text
product_id
available_stock
version
```

## Reset the database

```powershell
docker compose down -v
docker compose up -d
```

After recreating the volume, `init.sql` runs again. Reapply the schema evolution:

```powershell
Get-Content .\02-add-optimistic-concurrency.sql |
docker compose exec -T postgres psql `
  -U inventory_user `
  -d distributed_inventory
```

---

# PostgreSQL Docker Port Conflict

During `AtomicDatabaseUpdate`, the application returned:

```text
28P01: password authentication failed for user "inventory_user"
```

The configured credentials were correct. Port `5432` was already being used by another local PostgreSQL instance.

This was verified with:

```powershell
netstat -ano | findstr :5432
```

The application was therefore reaching the local PostgreSQL server instead of the Docker container.

The Docker mapping was changed to:

```yaml
ports:
  - "5433:5432"
```

and the application connection string now uses:

```text
Host=localhost;Port=5433
```

```text
Application
    │
    │ localhost:5433
    ▼
Docker
    │
    │ container:5432
    ▼
PostgreSQL 16
```

---

# Strategy Comparison

| Strategy | Coordination | Scope / behavior |
|---|---|---|
| `Naive` | None | Race condition / overselling |
| `LocalLock` | Local memory | Correct inside one process |
| `SharedResourceSynchronization` | Shared in-memory resource | Correct while all operations share that resource |
| `AtomicDatabaseUpdate` | PostgreSQL | Atomic condition + update |
| `OptimisticConcurrency` | PostgreSQL + `version` | Detects stale state |
| `OptimisticConcurrencyRetry` | PostgreSQL + `version` | Re-reads and re-evaluates after conflict |
| `DistributedLock` | PostgreSQL session | Serializes the critical section |
| `DistributedLockWithTimeout` | PostgreSQL session | Bounds the wait for exclusive access |

---


# Choosing a Concurrency Strategy

The scenarios in this exercise are not simply successive improvements where the last solution replaces all previous ones. They represent different coordination strategies with different trade-offs.

The main options explored were:

```text
Shared Resource Synchronization
Atomic Database Update
Optimistic Concurrency
Optimistic Concurrency + Retry
Distributed Lock
Distributed Lock + Timeout
```

The first question should therefore not be:

> Which concurrency mechanism is the most advanced?

but:

> What guarantee does this operation actually need?

## 1. Shared Resource Synchronization

```text
Prevent concurrent access
→ execute CHECK + UPDATE inside one critical section
```

Use it when all participants truly share the same synchronization mechanism and the critical section is short.

```text
Thread A ─┐
          ├─> Shared Resource ─> lock ─> CHECK + UPDATE
Thread B ─┘
```

It is simple and effective inside one process, but a local in-memory lock does not coordinate multiple application instances.

Typical fit:

- Shared in-memory state.
- Short critical sections.
- Frequent contention.
- Serialization is cheap.

---

## 2. Atomic Database Update

Before introducing explicit locks or version-based concurrency, ask whether the database can express the complete state transition atomically.

For the inventory decrement:

```sql
UPDATE product_inventory
SET available_stock = available_stock - @quantity
WHERE product_id = @productId
  AND available_stock >= @quantity;
```

The database performs:

```text
CHECK + UPDATE
```

as one atomic operation.

Typical fit:

- The business rule can be expressed directly in SQL.
- The operation is small and localized.
- The database is already the shared source of truth.
- There is no need for a long `READ → process → WRITE` workflow.

For this exercise, this is usually the simplest solution for the stock decrement itself.

---

## 3. Optimistic Concurrency

Optimistic concurrency allows multiple operations to work concurrently and detects stale state when writing.

```text
A reads version 10
B reads version 10

A performs business logic
B performs business logic

A writes → version 11
B writes → conflict
```

Typical fit:

- There is meaningful processing between `READ` and `WRITE`.
- Conflicts are expected to be relatively uncommon.
- Holding a lock while processing would be undesirable.
- A conflict can be detected and handled safely.

The operation is not serialized in advance. Instead, the system verifies that the state used to make the decision is still current.

---

## 4. Optimistic Concurrency + Retry

A retry is appropriate when a concurrency conflict can be recovered automatically.

```text
Conflict
   ↓
READ current state
   ↓
RE-EVALUATE business rules
   ↓
retry if still valid
```

The retry must never blindly repeat an update based on stale state.

Typical fit:

- Conflicts are transient.
- Re-reading is inexpensive.
- Business rules can be evaluated again.
- Re-executing the operation is safe.

A retry can still end in a business rejection:

```text
Concurrency conflict
        ↓
Read again
        ↓
Stock is now 0
        ↓
Reject purchase
```

---

## 5. Distributed Lock

A distributed lock coordinates multiple processes through a resource they all share.

In this exercise PostgreSQL advisory locks provide that coordination:

```text
Instance A ─┐
            ├─> PostgreSQL Advisory Lock
Instance B ─┘
```

Only the lock owner enters:

```text
READ → CHECK → UPDATE
```

Typical fit:

- Multiple application instances must execute a critical section exclusively.
- The operation cannot easily be represented as one atomic database statement.
- Allowing concurrent execution and resolving conflicts afterward is undesirable.
- The protected operation is important enough to justify explicit distributed coordination.

The trade-off is additional complexity and the possibility of waiting for the lock.

---

## 6. Distributed Lock + Timeout

A distributed lock introduces another question:

```text
What happens if the lock is unavailable?
```

Instead of waiting indefinitely:

```text
try lock
   ↓
busy
   ↓
retry
   ↓
timeout
```

This bounds waiting time and separates:

```text
Business failure
→ operation is not valid

Coordination failure
→ operation could not obtain exclusive access
```

Failure recovery and lock lifecycle must also be considered, which is why the exercise later tested `finally` and PostgreSQL session termination.

---

# Decision Map

A practical decision process for the strategies explored in this exercise is:

```text
                    START
                      │
                      ▼
        Is the shared state only in one process?
                │                 │
               YES                NO
                │                 │
                ▼                 ▼
      Is the critical        Is the database
      section short?         the shared source
          │                  of truth?
         YES                      │
          │                      YES
          ▼                       │
 Shared Resource                  ▼
 Synchronization       Can the complete state
                       transition be expressed
                       atomically in the DB?
                           │             │
                          YES            NO
                           │             │
                           ▼             ▼
                    Atomic Database   Is there a
                        Update        READ → PROCESS
                                      → WRITE flow?
                                        │       │
                                       YES      NO
                                        │       │
                                        ▼       ▼
                              Are conflicts   Does the
                              uncommon and    operation require
                              recoverable?    exclusive execution?
                                │      │          │
                               YES     NO         YES
                                │      │          │
                                ▼      │          ▼
                           Optimistic  │    Distributed Lock
                           Concurrency │          │
                                │      │          ▼
                                ▼      │    Add timeout /
                         Can conflict  │    failure recovery
                         be retried?   │
                           │    │      │
                          YES   NO     │
                           │    │      │
                           ▼    ▼      │
                       Retry   Surface │
                              conflict │
                                      │
                                      └──> Consider explicit
                                           distributed coordination
```

A shorter mental model is:

```text
1. Can the operation be atomic at the shared resource?
   → YES: prefer Atomic Database Update.

2. Do I need READ → business logic → WRITE?
   → YES: consider Optimistic Concurrency.

3. If optimistic conflict occurs, can I safely re-read and retry?
   → YES: Optimistic Concurrency + Retry.

4. Must only one distributed instance execute the critical section?
   → YES: Distributed Lock.

5. Can waiting for that lock be bounded?
   → usually YES: add Timeout + Failure Recovery.

6. Is everything inside one process and the critical section is short?
   → a local/shared lock may be enough.
```

## Strategy Summary

| Situation | Preferred starting point |
|---|---|
| Shared state only inside one process | Shared Resource Synchronization |
| Simple conditional change in shared database | Atomic Database Update |
| `READ → business logic → WRITE` with uncommon conflicts | Optimistic Concurrency |
| Optimistic conflict can be safely re-evaluated | Optimistic Concurrency + Retry |
| Multiple instances require exclusive execution | Distributed Lock |
| Distributed lock must not wait indefinitely | Distributed Lock + Timeout |
| Lock owner may fail | `try/finally` + session lifecycle / failure recovery |

The important principle is to choose the **simplest mechanism that provides the required guarantee**. A distributed lock is not automatically better than an atomic database update, and optimistic concurrency is not automatically better than locking. Each solves a different shape of concurrency problem.


# Core Idea

```text
Shared mutable state
        ↓
Local synchronization
        ↓
Multiple instances break local synchronization
        ↓
Move coordination to a shared resource
        ↓
Database atomicity
        ↓
Optimistic conflict detection
        ↓
Retry with fresh state
        ↓
Explicit distributed coordination
```

There is no single concurrency strategy that is best for every problem.

For a simple stock decrement, `AtomicDatabaseUpdate` solves the problem with very little complexity.

Optimistic concurrency becomes useful when the operation has a broader:

```text
READ → business logic → WRITE
```

lifecycle and we need to detect whether the state used by the business logic became stale.

A distributed lock is useful when multiple instances must explicitly coordinate exclusive access to a distributed critical section.

The central lesson of the exercise is:

> **Synchronization can coordinate all participants only when the coordination mechanism is available to all of them.**

Moving from concurrency inside one process to distributed concurrency changes precisely where that coordination boundary must live.
