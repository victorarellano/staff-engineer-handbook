# Bank Account Race Condition

## 1. Problem Context

A bank account has a balance that may be accessed by multiple withdrawal operations concurrently.

The business rule is simple:

> A withdrawal can only be accepted when the account has sufficient funds.

The scenario used in this exercise is:

```text
Initial balance: 1000

Withdrawal A: 700
Withdrawal B: 500
```

Individually, each withdrawal is valid:

```text
1000 >= 700
1000 >= 500
```

But both withdrawals cannot be accepted because:

```text
700 + 500 = 1200
```

while the account contains only:

```text
1000
```

The problem appears when both operations read and validate the same balance before either one completes its update.

---

## 2. The Concurrency Problem

A simple withdrawal implementation could be:

```csharp
if (Balance < amount)
    return false;

Balance -= amount;

return true;
```

When operations execute sequentially, the logic behaves as expected.

Under concurrent execution, however, the withdrawal is actually a sequence of operations:

```text
READ balance
    ↓
CHECK sufficient funds
    ↓
WRITE new balance
```

If two withdrawals execute this sequence concurrently, their steps may become interleaved.

The required guarantee is:

> The validation of available funds and the corresponding balance update must behave as one consistent state transition.

---

## 3. Shared Mutable State

The shared state in this exercise is:

```text
Balance
```

Both withdrawal operations access and potentially modify the same value:

```text
Withdrawal A ──┐
               │
               ▼
             Balance
               ▲
               │
Withdrawal B ──┘
```

This is **shared mutable state**:

- The state is shared by multiple concurrent operations.
- The state can change.
- The result of one operation affects the validity of another.

Without coordination, both operations may make decisions based on the same previous state.

---

## 4. Initial Unsafe Implementation

The first implementation intentionally contains no synchronization.

Its purpose is to reproduce and observe the concurrency problem before introducing a solution.

A simplified version is:

```csharp
public async Task<bool> WithdrawUnsafeAsync(
    string operation,
    decimal amount,
    CancellationToken cancellationToken)
{
    _logger.LogInformation(
        "{Operation} reads balance {Balance}",
        operation,
        Balance);

    if (Balance < amount)
        return false;

    await Task.Delay(100, cancellationToken);

    Balance -= amount;

    return true;
}
```

The artificial delay increases the time window between validation and modification:

```text
READ
  ↓
CHECK
  ↓
WAIT
  ↓
WRITE
```

This makes it easier for another withdrawal to enter the same logical operation before the first one updates the balance.

The delay exists only to make the race condition easier to reproduce in the laboratory. It is not part of the business requirement.

---

## 5. Reproducing the Problem

The simulation starts two withdrawals against the same account:

```text
Initial balance = 1000

Withdrawal A = 700
Withdrawal B = 500
```

The observed execution was:

```text
Withdrawal A reads balance 1000
Withdrawal A validates withdrawal of 700

Withdrawal B reads balance 1000
Withdrawal B validates withdrawal of 500

Withdrawal A writes new balance 300
Withdrawal B writes new balance -200
```

Both operations returned success:

```text
Withdrawal A result: True
Withdrawal B result: True
Final balance: -200
```

The result violates the business rule.

Both withdrawals were approved because both validated themselves against the same original balance.

---

## 6. Understanding the Interleaving

The execution can be represented as:

```text
Withdrawal A              Withdrawal B
------------              ------------

READ 1000

                          READ 1000

CHECK 1000 >= 700

                          CHECK 1000 >= 500

WRITE 300

                          WRITE -200
```

The critical observation is that:

```text
A validated using Balance = 1000
B validated using Balance = 1000
```

Neither validation considered the effect of the other withdrawal.

The operations were individually correct but collectively inconsistent.

---

## 7. Race Condition

This behavior is a **race condition**.

In this scenario, multiple operations:

1. Execute concurrently.
2. Access the same mutable state.
3. Make decisions based on that state.
4. Modify that state.
5. Produce a result that depends on their relative execution timing.

The problematic relationship is:

```text
Concurrent operations
        +
Shared mutable state
        +
Uncoordinated read/check/write
        =
Race condition
```

An important characteristic of race conditions is that they can be timing-dependent.

An unsafe implementation may sometimes produce a correct result simply because the operations happened to execute in a favorable order.

That does not make the implementation safe.

---

## 8. Finding the Critical Section

Once the race condition is understood, the next question is:

> Which operations must be protected from concurrent execution?

It may initially appear that only this statement needs protection:

```csharp
Balance -= amount;
```

But that would not solve the actual problem.

The withdrawal decision depends on both:

```csharp
if (Balance < amount)
    return false;
```

and:

```csharp
Balance -= amount;
```

Therefore, the critical section is the complete state transition:

```text
READ current balance
        ↓
CHECK sufficient funds
        ↓
UPDATE balance
```

Conceptually:

```text
ENTER CRITICAL SECTION
        │
        ▼
READ
        │
        ▼
VALIDATE
        │
        ▼
MODIFY
        │
        ▼
EXIT CRITICAL SECTION
```

No competing withdrawal should execute that same state transition against the same account while another withdrawal is inside it.

---

## 9. Atomicity

The business operation should behave as if:

```text
CHECK sufficient funds
        +
UPDATE balance
```

were one indivisible operation with respect to competing withdrawals.

This is the required **atomicity**.

Atomicity does not mean that the source code must contain a single statement.

Instead, several statements may need to behave as one logical unit:

```text
Before withdrawal
        │
        ▼
[ CHECK + UPDATE ]
        │
        ▼
After withdrawal
```

Another concurrent withdrawal should make its decision using either the state before that transition or the state after it.

It should not independently validate itself using state that another concurrent withdrawal is already in the process of changing.

---

## 10. Protecting the Critical Section

The corrected implementation introduces an object used to synchronize access to the balance transition:

```csharp
private readonly object _balanceLock = new();
```

The critical section is then protected with:

```csharp
lock (_balanceLock)
{
    if (Balance < amount)
        return false;

    Balance -= amount;

    return true;
}
```

Only one execution can hold that lock and execute the protected section at a time.

The resulting behavior becomes:

```text
Withdrawal A
    │
    ▼
acquires lock
    │
    ▼
READ 1000
    │
    ▼
CHECK 1000 >= 700
    │
    ▼
WRITE 300
    │
    ▼
releases lock


Withdrawal B
    │
    ▼
acquires lock
    │
    ▼
READ 300
    │
    ▼
CHECK 300 >= 500
    │
    ▼
rejected
```

---

## 11. Corrected Result

The observed synchronized execution was:

```text
Withdrawal A attempts withdrawal of 700
Withdrawal B attempts withdrawal of 500

Withdrawal A reads balance 1000
Withdrawal A validates withdrawal of 700
Withdrawal A writes new balance 300

Withdrawal B reads balance 300
Withdrawal B rejected. Balance 300, requested 500
```

Final result:

```text
Withdrawal A result: True
Withdrawal B result: False
Final balance: 300
```

The account invariant is preserved.

---

## 12. What the `lock` Actually Does

The `lock` does not implement the insufficient-funds rule.

It does not know:

- what a bank account is,
- what a withdrawal means,
- whether 300 is enough to withdraw 500,
- which withdrawal should succeed.

Its responsibility is narrower:

> Prevent multiple executions from entering the protected critical section at the same time for the same synchronization object.

Therefore:

```text
Withdrawal A enters critical section
        │
Withdrawal B cannot enter simultaneously
        │
Withdrawal A updates Balance
        │
Withdrawal A leaves critical section
        │
Withdrawal B enters
        │
Withdrawal B sees the updated Balance
        │
Business rule evaluates the current state
```

The second withdrawal is rejected by the business rule because it sees:

```text
Balance = 300
```

The lock makes that correct observation possible by preventing the two state transitions from overlapping.

---

## 13. Waiting for the Lock

If one thread owns `_balanceLock`, another thread attempting to enter:

```csharp
lock (_balanceLock)
```

must wait until the lock becomes available.

Conceptually:

```text
Thread A
   │
   ▼
acquires _balanceLock
   │
   ▼
executes critical section


Thread B
   │
   ▼
tries to acquire _balanceLock
   │
   ▼
waits
   │
   ▼
Thread A releases
   │
   ▼
Thread B can acquire
```

A traditional C# `lock` is a synchronous mutual-exclusion mechanism.

The waiting thread does not continue through the protected code until it obtains the lock.

This is one reason critical sections should remain focused on the state transition that actually requires exclusive access.

---

## 14. Keeping the Critical Section Focused

The artificial delay used to reproduce the race condition does not belong inside the critical section.

The synchronized experiment keeps asynchronous waiting outside the lock:

```csharp
await Task.Delay(100, cancellationToken);

lock (_balanceLock)
{
    // read
    // validate
    // update
}
```

The protected region contains only the operations whose consistency depends on exclusive access to `Balance`.

Conceptually:

```text
Other work / waiting
        │
        ▼
ENTER LOCK
        │
        ▼
READ
CHECK
UPDATE
        │
        ▼
EXIT LOCK
```

This reduces unnecessary contention and makes the synchronization boundary easier to understand.

C# also does not allow `await` directly inside a standard `lock` statement.

---

## 15. Unsafe and Safe Implementations

The laboratory intentionally preserves both versions:

```text
WithdrawUnsafeAsync
        │
        └── exposes the race condition

WithdrawSafeAsync
        │
        └── protects the state transition
```

The unsafe implementation is retained as learning evidence.

The experiment can therefore demonstrate the complete reasoning process:

```text
Naive implementation
        ↓
Concurrent execution
        ↓
Incorrect result
        ↓
Identify shared state
        ↓
Identify race condition
        ↓
Find critical section
        ↓
Define atomicity requirement
        ↓
Protect critical section
        ↓
Correct result
```

In production code, the unsafe version would normally not be maintained as an alternative implementation.

---

## 16. Comparing the Executions

### Unsafe execution

```text
A READ 1000
A CHECK ✓

B READ 1000
B CHECK ✓

A WRITE 300
B WRITE -200
```

Result:

```text
A = accepted
B = accepted
Balance = -200
```

The business invariant is violated.

### Synchronized execution

```text
A ACQUIRE

A READ 1000
A CHECK ✓
A WRITE 300

A RELEASE

B ACQUIRE

B READ 300
B CHECK ✗

B RELEASE
```

Result:

```text
A = accepted
B = rejected
Balance = 300
```

The business invariant is preserved.

---

## 17. Execution Order Is Not the Guarantee

The synchronized solution does not guarantee that Withdrawal A always executes first.

Another valid execution could be:

```text
Withdrawal B acquires lock
        │
        ▼
READ 1000
        │
        ▼
CHECK 1000 >= 500
        │
        ▼
WRITE 500
        │
        ▼
release lock

Withdrawal A acquires lock
        │
        ▼
READ 500
        │
        ▼
CHECK 500 >= 700
        │
        ▼
rejected
```

Final result:

```text
Withdrawal A result: False
Withdrawal B result: True
Final balance: 500
```

This result is also correct.

The required guarantee is not:

```text
A must execute before B
```

It is:

```text
A and B must not perform the balance
state transition concurrently.
```

Correctness must not depend on which operation happens to acquire the lock first.

---

## 18. Synchronization Scope

The synchronization object belongs to the account:

```csharp
private readonly object _balanceLock = new();
```

This means the protected resource is the state of that specific account.

Conceptually:

```text
BankAccount A
   │
   └── its Balance
       its lock

BankAccount B
   │
   └── its Balance
       its lock
```

Operations against the same account compete for the same synchronization boundary.

Independent account instances can have independent synchronization boundaries.

Choosing the correct synchronization scope is therefore part of the design.

A lock that is too broad may serialize unrelated work.

A lock that is too narrow may fail to protect the complete invariant.

---

## 19. Key Lessons

### 1. Shared mutable state requires careful coordination

When multiple concurrent operations read and modify the same state, their possible interleavings must be considered.

### 2. Sequential correctness does not guarantee concurrent correctness

Code that behaves correctly when called one operation at a time may fail when executions overlap.

### 3. A race condition is about timing and shared state

The incorrect result appears because both operations make decisions using the same stale balance.

### 4. The critical section follows the business invariant

The critical section is:

```text
READ
CHECK
WRITE
```

because those steps collectively determine whether the balance remains valid.

### 5. Atomicity applies to logical operations

Several statements may need to behave as one indivisible state transition.

### 6. `lock` provides mutual exclusion

It prevents concurrent execution of the protected section for the same synchronization object.

### 7. Synchronization and business logic have different responsibilities

The lock controls access.

The withdrawal rule determines whether sufficient funds exist.

### 8. Correctness must not depend on scheduling order

Either withdrawal may execute first. The invariant must remain valid in both cases.

### 9. Race conditions may be difficult to reproduce

The artificial delay makes the problematic interleaving easier to observe, but the underlying defect exists even if a particular execution happens to produce the expected result.

### 10. Keep synchronization boundaries explicit

The code should make clear which state is protected and which operations belong to the critical section.

---

## 20. Experiments

### Experiment 1 — Reverse the Start Order

Start Withdrawal B before Withdrawal A.

Observe which operation obtains the critical section first and verify that the final state remains valid.

### Experiment 2 — Repeat the Unsafe Scenario

Run the unsafe scenario multiple times.

Observe whether execution timing changes the result.

The objective is to see that concurrency bugs may be nondeterministic.

### Experiment 3 — Add More Withdrawals

For example:

```text
Initial balance: 1000

A: 300
B: 250
C: 400
D: 200
```

Compare the unsafe and synchronized versions.

### Experiment 4 — Add Deposits

Introduce concurrent deposits that also modify `Balance`.

Determine which operations must share the same synchronization boundary.

### Experiment 5 — Multiple Accounts

Create multiple independent accounts and observe the effect of account-level synchronization.

This helps explore synchronization scope and unnecessary contention.

---

## 21. Possible Evolution

The current problem contains one shared resource:

```text
Balance
```

A natural future extension is an operation that needs to coordinate more than one shared resource, such as transferring money between accounts.

That introduces additional questions:

- What happens when an operation needs multiple synchronization boundaries?
- In which order should resources be acquired?
- What happens if two operations wait for resources held by each other?
- How can synchronization design itself create new concurrency problems?

Those questions belong to subsequent concurrency exercises rather than this one.

---

## 22. Mental Model

The reusable model from this exercise is:

```text
Multiple concurrent operations
          +
Shared mutable state
          +
Read / validate / modify sequence
          =
Potential race condition
```

The reasoning process is:

```text
Identify shared state
        ↓
Identify concurrent operations
        ↓
Define the invariant
        ↓
Observe possible interleavings
        ↓
Identify the critical section
        ↓
Define required atomicity
        ↓
Choose synchronization
        ↓
Verify all valid execution orders
```

---

## 23. Guiding Principle

The main lesson is not:

```text
Use lock around Balance.
```

The reusable principle is:

> When the correctness of a state transition depends on several related reads, validations, and writes, concurrent operations must not be allowed to interleave those steps in a way that violates the invariant.

The synchronization mechanism is chosen after identifying that requirement.
