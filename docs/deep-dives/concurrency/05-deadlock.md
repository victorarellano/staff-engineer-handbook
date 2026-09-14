# Bank Transfer Deadlock

## 1. Problem Context

A deadlock can appear when concurrent operations need exclusive access to multiple resources and acquire them in incompatible orders.

This exercise models two bank accounts and two concurrent transfers:

```text
Transfer 1: Account A → Account B
Transfer 2: Account B → Account A
```

Each transfer must protect both accounts while moving money.

The central question is:

> How can concurrent operations become permanently blocked even though every individual lock is working correctly?

The exercise evolves through:

```text
Naive nested locking
        ↓
Reproduce deadlock
        ↓
Understand deadlock conditions
        ↓
Ordered locking
        ↓
Timed lock acquisition
        ↓
Retry + backoff
        ↓
Jitter
```

---

## 2. Naive Nested Locking

The first implementation acquires the source account lock and then the destination account lock.

```csharp
lock (sourceAccount.SyncRoot)
{
    lock (destinationAccount.SyncRoot)
    {
        sourceAccount.Withdraw(amount);
        destinationAccount.Deposit(amount);
    }
}
```

For opposite transfers, the acquisition order is also opposite:

```text
Transfer A → B          Transfer B → A

lock A                  lock B
   ↓                       ↓
lock B                  lock A
```

A deliberate delay after acquiring the first lock makes the problematic interleaving observable:

```text
Transfer A → B          Transfer B → A

acquires A              acquires B
    │                       │
waits for B             waits for A
```

Neither operation can continue, and neither can release its first lock because it is waiting inside that lock for the second one.

The application does not need to throw an exception. The threads can simply remain blocked indefinitely.

---

## 3. Why the Deadlock Exists

Four conditions coexist in this scenario.

### Mutual Exclusion

Each account lock can have only one owner at a time.

### Hold and Wait

Each transfer keeps its first lock while waiting for the second.

```text
holds A + waits for B
holds B + waits for A
```

### No Preemption

A transfer cannot forcibly take a lock from another transfer. The current owner must release it.

### Circular Wait

The dependencies form a cycle:

```text
Transfer A→B owns A
        ↓
     waits for B
        ↓
Transfer B→A owns B
        ↓
     waits for A
        └──────────────┐
                       ↓
                Transfer A→B
```

A useful mental model is:

> Each operation is waiting for something that can only be released by another operation that is also waiting.

The individual locks are working correctly. The problem is the relationship between multiple resource acquisitions.

---

## 4. Two Perspectives of the Transfer

The solution becomes clearer when the operation is viewed from two independent perspectives.

### Business perspective

The transfer direction determines where the money moves:

```text
sourceAccount      → money leaves
destinationAccount → money arrives
```

For example:

```text
Transfer B → A

sourceAccount      = B
destinationAccount = A
```

### Concurrency perspective

Synchronization determines the order in which account locks are acquired.

That order does not need to match the direction of the money.

```text
Business:    B → A
Lock order:  A → B
```

Therefore:

```text
BUSINESS                  CONCURRENCY

sourceAccount             accountsInLockOrder
destinationAccount        firstLockAccount
                          secondLockAccount
```

This separation is central to the ordered-locking solution.

---

## 5. Preventing Deadlock with Ordered Locking

Every transfer follows one global synchronization rule:

> Acquire account locks in ascending Account Id order.

```csharp
var accountsInLockOrder = new[]
{
    sourceAccount,
    destinationAccount
}
.OrderBy(account => account.Id)
.ToArray();

var firstLockAccount = accountsInLockOrder[0];
var secondLockAccount = accountsInLockOrder[1];

lock (firstLockAccount.SyncRoot)
{
    lock (secondLockAccount.SyncRoot)
    {
        sourceAccount.Withdraw(amount);
        destinationAccount.Deposit(amount);
    }
}
```

Suppose:

```text
Account A → Id 1
Account B → Id 2
```

Both business directions now use the same synchronization order:

```text
Transfer A → B

Business:   A → B
Lock order: A → B


Transfer B → A

Business:   B → A
Lock order: A → B
```

Both concurrent operations must first compete for A:

```text
Transfer 1                 Transfer 2

acquires A                 waits for A
    ↓
acquires B
    ↓
performs transfer
    ↓
releases locks
                               ↓
                         can continue
```

The circular dependency cannot form.

Ordered locking prevents this deadlock by eliminating the **circular wait** condition.

---

## 6. Timed Lock Acquisition

The exercise also explores a different situation:

> What if a global acquisition order cannot be guaranteed?

Instead of waiting indefinitely for the second lock, an operation can attempt to acquire it for a bounded period using `Monitor.TryEnter`.

```csharp
var secondLockTaken = false;

try
{
    Monitor.TryEnter(
        destinationAccount.SyncRoot,
        timeoutMilliseconds,
        ref secondLockTaken);

    if (!secondLockTaken)
        return;

    sourceAccount.Withdraw(amount);
    destinationAccount.Deposit(amount);
}
finally
{
    if (secondLockTaken)
        Monitor.Exit(destinationAccount.SyncRoot);
}
```

The flow becomes:

```text
Acquire first lock
        ↓
Try second lock
        │
        ├── success → transfer
        │
        └── timeout → abandon attempt
                           ↓
                    release first lock
```

This does not remove the possibility of conflicting acquisition orders. It prevents the operation from waiting forever for the second lock.

---

## 7. Retrying After a Failed Acquisition

Abandoning a transfer after the first timeout may be too aggressive.

The operation can release what it acquired and retry.

```text
Attempt
   ↓
Acquire first lock
   ↓
Try second lock
   │
   ├── success → transfer
   │
   └── timeout
          ↓
   release first lock
          ↓
        retry
```

The important rule is:

> A retry must begin only after releasing the resources acquired by the previous attempt.

A bounded number of attempts prevents endless retries.

---

## 8. Livelock

If both transfers retry with identical timing, they can repeatedly react to each other without completing:

```text
Transfer A → B          Transfer B → A

acquires A              acquires B
times out               times out
releases A              releases B

waits 200 ms            waits 200 ms

acquires A              acquires B
times out               times out
...
```

The distinction is:

```text
Deadlock
────────
operations are blocked
no progress


Livelock
────────
operations remain active
no progress
```

The goal is therefore not merely activity. Concurrent operations must make **progress**.

---

## 9. Retry with Backoff

Backoff reduces how aggressively an operation retries.

A simple progressive backoff is:

```csharp
var retryDelay =
    _settings.RetryDelayMilliseconds * attempt;

Thread.Sleep(retryDelay);
```

For a base delay of `200 ms`:

```text
Attempt 1 fails → wait 200 ms
Attempt 2 fails → wait 400 ms
Attempt 3 fails → wait 600 ms
```

However, deterministic backoff can preserve symmetry:

```text
T1 waits 200 ms        T2 waits 200 ms
T1 retries             T2 retries

T1 waits 400 ms        T2 waits 400 ms
T1 retries             T2 retries
```

This behavior was observed in the simulation: both transfers exhausted their three attempts while following the same retry rhythm.

---

## 10. Breaking Retry Symmetry with Jitter

The final refinement adds a small random variation to the backoff:

```csharp
var baseDelay =
    _settings.RetryDelayMilliseconds * attempt;

var jitter =
    Random.Shared.Next(
        0,
        _settings.RetryDelayMilliseconds);

var retryDelay =
    baseDelay + jitter;

Thread.Sleep(retryDelay);
```

Now the competing transfers can wait different periods:

```text
Transfer A → B → 263 ms
Transfer B → A → 347 ms
```

One operation can wake first, acquire both locks, complete, and release them before the other retries.

```text
Attempt 1
   ↓
collision
   ↓
timeout
   ↓
release
   ↓
backoff + jitter
   ↓
different retry times
   ↓
one operation progresses
```

In the exercise, adding jitter allowed progress on the second attempt after deterministic retries had failed.

Jitter does **not** structurally prove that a deadlock cannot occur. It changes timing and improves the probability of progress.

---

## 11. Comparing the Strategies

| Strategy | Purpose | Main Property | Effectiveness |
|---|---|---|---|
| Naive nested locking | Demonstrate the problem | Can deadlock | None — exposes the deadlock |
| Ordered locking | Prevent circular wait | Structural prevention | **High — prevents this deadlock by design** |
| `Monitor.TryEnter` + timeout | Bound lock waiting | Avoid indefinite acquisition | **Medium — avoids permanent blocking but may abort the operation** |
| Retry | Try the operation again | Recovery after failed acquisition | **Medium — may eventually succeed, but can repeatedly collide** |
| Backoff | Reduce repeated contention | Less aggressive retry | **Medium — reduces collisions but does not guarantee progress** |
| Jitter | Break synchronized retries | Improves probability of progress | **Medium-High — improves progress under contention, but remains probabilistic** |

> Effectiveness is evaluated in the context of this exercise. It represents how strongly each strategy addresses the demonstrated deadlock, not a universal ranking of concurrency mechanisms.

The central contrast is:

```text
ORDERED LOCKING
      ↓
change acquisition rules
      ↓
circular wait cannot form


TIMEOUT + RETRY + BACKOFF + JITTER
      ↓
allow conflicting acquisition attempts
      ↓
stop waiting
      ↓
release
      ↓
try again
```

For this two-account scenario, ordered locking is the stronger solution because a stable global resource order can be defined.

---

## 12. Final Mental Model

When an operation needs multiple exclusive resources, ask:

```text
Do multiple operations need
the same resources?
        │
        ▼
Can they acquire them
in different orders?
        │
        ├── Yes
        │     ↓
        │ possible circular wait
        │     ↓
        │ possible deadlock
        │
        └── No
              ↓
        circular wait removed
```

A practical prevention rule is:

> When multiple locks must be acquired, define a global acquisition order and make every operation follow it whenever the problem permits that strategy.

If this cannot be guaranteed, bounded acquisition and retry policies can prevent indefinite waiting, but they introduce additional concerns such as retry exhaustion, livelock, backoff, and fairness.

---

## 13. Conclusion

This exercise started with two individually valid nested-lock operations:

```text
A → B locks A then B
B → A locks B then A
```

When executed concurrently, they created a circular dependency and reproduced a deadlock.

The key lessons are:

1. A deadlock can occur even when each synchronization primitive works correctly.
2. Deadlock is a relationship between concurrent operations and the resources they hold and request.
3. Mutual exclusion, hold-and-wait, no preemption, and circular wait coexist in the reproduced scenario.
4. A global lock order prevents this deadlock by eliminating circular wait.
5. Business direction and synchronization order are separate concerns.
6. Timed acquisition can prevent indefinite waiting.
7. Retry must release previously acquired resources before starting a new attempt.
8. Active retries without progress can lead to livelock.
9. Backoff reduces retry pressure, while jitter helps break synchronized retry patterns.
10. Structural prevention, when available, is stronger than relying on timing and retries for progress.

The central idea is not simply to avoid nested locks. It is to reason about the complete resource-acquisition relationship and ensure that concurrent operations cannot create an unresolvable cycle.
