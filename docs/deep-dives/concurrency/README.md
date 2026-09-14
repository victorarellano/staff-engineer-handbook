# Concurrency

This section explores concurrency through practical problems and executable examples.

The goal is not to learn isolated synchronization primitives or framework APIs, but to understand the problems that appear when multiple operations execute concurrently, how to recognize them, and how to choose an appropriate coordination strategy.

Each deep dive starts from a concrete scenario, identifies the concurrency problem behind it, analyzes possible approaches, and connects the reasoning with an implementation available in the handbook.

---

## Learning Goals

The main goals of this section are to develop the ability to:

- Recognize concurrency problems in real-world scenarios.
- Distinguish concurrency, parallelism, and asynchronous execution.
- Identify shared resources and synchronization boundaries.
- Understand race conditions and atomicity.
- Coordinate multiple concurrent operations safely.
- Limit access to finite resources.
- Understand blocking versus asynchronous waiting.
- Identify and prevent deadlocks.
- Understand producer-consumer scenarios and backpressure.
- Reason about cancellation, failures, and resource cleanup.
- Evaluate trade-offs between different synchronization mechanisms.
- Extend concurrency reasoning from a single process to distributed systems.

The emphasis is on reasoning about behavior and trade-offs rather than memorizing APIs.

### Learning Goals Coverage

**Coverage:** ✓ Covered · ◐ Introduced / partially covered · — Not covered

| Learning Goal | 01 Butcher Shop | 02 Bank Account | 03 Producer-Consumer | 04 Resource Pool | 05 Deadlock | 06 Rate Limiting | 07 Distributed Inventory |
|---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| Recognize concurrency problems in real-world scenarios | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Distinguish concurrency, parallelism, and asynchronous execution | ◐ | — | ✓ | ✓|  ◐ | ✓ | ✓ |
| Identify shared and limited resources | ✓ | ✓ | ✓ | ✓  ✓ | ✓ | ✓ |
| Identify critical sections and synchronization boundaries | ◐ | ✓ | ◐ | ✓ | ✓ | ◐ | ✓ |
| Understand race conditions and atomicity | — | ✓ | ◐ | ✓ | ◐ | — | ✓ |
| Coordinate multiple concurrent operations safely | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Limit concurrent access to finite resources | ✓ | — | — | ✓ | — | — | — |
| Understand blocking versus asynchronous waiting | ✓ | ◐ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Identify and prevent deadlocks | — | — | — | —  ✓ | — | — |
| Understand producer-consumer scenarios and backpressure | — | — | ✓ | — | — | ◐ | — |
| Reason about cancellation, failures, and resource cleanup | ◐ | ◐ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Evaluate trade-offs between synchronization mechanisms | ◐ | ◐ | ✓ | ✓|  ✓ | ✓ | ✓ |
| Extend concurrency reasoning to distributed systems | — | — | — | — | — | ◐ | ✓ |

---

## Learning Approach

Each concurrency problem follows approximately the same reasoning process:

```text
Real-world scenario
        ↓
Identify concurrent actors
        ↓
Identify shared or limited resources
        ↓
Determine possible conflicts
        ↓
Define required guarantees
        ↓
Evaluate synchronization strategies
        ↓
Implement the solution
        ↓
Observe its behavior
        ↓
Analyze limitations and alternatives
```

Whenever possible, implementations are kept small enough to make the concurrency behavior observable and understandable.

## Topics

The deep dives progressively explore concepts such as:

### Concurrency Fundamentals
- Concurrent execution
- Parallel execution
- Asynchronous execution
- Shared state
- Critical sections

### Synchronization
- Mutual exclusion
- Locks
- Semaphores
- Atomic operations
- Coordination between tasks

### Concurrency Problems
- Race conditions
- Lost updates
- Deadlocks
- Starvation
- Resource contention

### Coordination Patterns
- Limited concurrency
- Producer-consumer
- Resource pools
- Backpressure
- Rate limiting

### Operational Concerns
- Cancellation
- Timeouts
- Failure handling
- Resource cleanup
- Observability of concurrent systems

### Distributed Concurrency

Later exercises extend these ideas beyond a single process:

- Optimistic concurrency
- Idempotency
- Distributed coordination
- Message queues
- Event-driven processing
- Consistency trade-offs

### Topic Coverage
**Coverage:** ✓ Covered · ◐ Introduced / partially covered · — Not covered

| Area | Topic | 01 Butcher Shop | 02 Bank Account | 03 Producer-Consumer | 04 Resource Pool | 05 Deadlock| 06 Rated Limited Service | 07 Distributed Inventory |
|---|---|---|---|---|---|---|---|---|
| Fundamentals | Concurrent execution | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Fundamentals | Parallel execution | — | — | — | — | — | — | — |
| Fundamentals | Asynchronous execution | ✓ | ◐ | ✓ | ✓ | ◐ | ✓ | ✓ |
| Fundamentals | Shared mutable state | ◐ | ✓ | ✓ | ✓ | ✓ | ◐ | ✓ |
| Fundamentals | Critical sections | — | ✓ | ◐ | ✓ | ✓ | ◐ | ✓ |
| Synchronization | Mutual exclusion | — | ✓ | ◐ | ✓ | ✓ | — | ✓ |
| Synchronization | Locks | — | ✓ | ◐ | ✓ | ✓ | — | ✓ |
| Synchronization | Semaphores | ✓ | — | — | ✓ | — | ✓ | — |
| Synchronization | Atomic operations | ✓ | — | — | — | — | — | ✓ |
| Synchronization | Task coordination | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Concurrency Problems | Race conditions | — | ✓ | ◐ | ✓ | ◐ | — | ✓ |
| Concurrency Problems | Lost updates | — | ◐ | — | — | — | — | ✓ |
| Concurrency Problems | Deadlocks | — | — | — | — | ✓ | — | — |
| Concurrency Problems | Starvation | — | — | — | — | — | — | — |
| Concurrency Problems | Resource contention | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Coordination Patterns | Limited concurrency | ✓ | — | — | ✓ | — | ✓ | — |
| Coordination Patterns | Producer-consumer | — | — | ✓ | — | — | ◐ | — |
| Coordination Patterns | Resource pools | ◐ | — | — | ✓ | — | — | — |
| Coordination Patterns | Backpressure | — | — | ✓ | — | — | ◐ | — |
| Coordination Patterns | Rate limiting | — | — | ◐ | — | — | ✓ | — |
| Operational Concerns | Cancellation | ✓ | ◐ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Operational Concerns | Timeouts | — | — | — | ✓ | ✓ | ◐ | ✓ |
| Operational Concerns | Failure handling | ◐ | ◐ | ◐ | ✓ | ✓ | ✓ | ✓ |
| Operational Concerns | Resource cleanup | ✓ | — | ✓ | ✓ | ✓ | ◐ | ✓ |
| Operational Concerns | Observability | ✓ | ✓ | ✓ | ✓ |  ✓ | ✓ | ✓ |
| Distributed Concurrency | Optimistic concurrency | — | — | — | — | — | — | ✓ |
| Distributed Concurrency | Idempotency | — | — | — | — | — | — | — |
| Distributed Concurrency | Distributed coordination | — | — | — | — | — | ◐ | ✓ |
| Distributed Concurrency | Message queues | — | — | ◐ | — | — | ◐ | — |
| Distributed Concurrency | Event-driven processing | — | — | ◐ | — | — | ◐ | — |
| Distributed Concurrency | Consistency trade-offs | — | — | — | — | — | ◐ | ✓ |

---

### Deep Dives


| # | Problem | Main Concepts | Status |
|---|---------|---------------|--------|
|01 |Butcher Shop Simulation|Limited concurrency, SemaphoreSlim, asynchronous waiting, cancellation|Completed|
|02 |Bank Account Race Condition |Shared mutable state, race condition, critical section, atomicity, `lock` | Completed |
|03 |Producer-Consumer Problem |Producer-consumer, `Channel<T>`, asynchronous waiting, completion, bounded capacity, backpressure | Completed |
|04 |Delivery Vehicle Resource Pool | Resource pool, `SemaphoreSlim`, `lock`, asynchronous acquisition, resource lifecycle, timeout, resource leak | Completed |
|05 |Bank Transfer Deadlock | Deadlock, nested locks, lock ordering, `Monitor.TryEnter`, timeout, retry, backoff, jitter, livelock | Completed |
|06 |Rate-Limited Services | Rate limiting, Fixed Window, Sliding Window, Token Bucket, retry, safety margin | Completed |
|07 |Distributed Inventory | Atomic database update, optimistic concurrency, retry, PostgreSQL advisory locks, timeout, failure recovery | Completed |

Additional problems will be added incrementally as new concurrency concepts are explored.

## Concurrency Coverage Matrix

This matrix provides a quick view of the main concurrency concepts explored by each deep dive.

| Concept | 01 | 02 | 03 | 04 | 05 | 06 | 07 |
|---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| `SemaphoreSlim` | ✓ | — | — | ✓ | — | ✓ | — |
| `lock` | — | ✓ | — | ✓ | ✓ | — | ✓ |
| Race conditions | — | ✓ | — | ✓ | ◐ | — | ✓ |
| Producer-consumer | — | — | ✓ | — | — | ◐ | — |
| Resource pool | ◐ | — | — | ✓ | — | — | — |
| Resource lifecycle / cleanup | ◐ | — | ◐ | ✓ | ✓ | ◐ | ✓ |
| Timeout | — | — | — | ✓ | ✓ | ◐ | ✓ |
| Backpressure | — | — | ✓ | — | — | ◐ | — |
| Deadlock | — | — | — | — | ✓ | — | — |
| Lock ordering | — | — | — | — | ✓ | — | — |
| Timed lock acquisition | — | — | — | — | ✓ | — | — |
| Retry / Backoff / Jitter | — | — | — | — | ✓ | ✓ | ✓ |
| Livelock | — | — | — | — | ✓ | — | — |
| Rate limiting | — | — | ◐ | — | — | ✓ | — |
| Fixed / Sliding Window | — | — | — | — | — | ✓ | — |
| Token Bucket | — | — | — | — | — | ✓ | — |
| Atomic database update | — | — | — | — | — | — | ✓ |
| Optimistic concurrency | — | — | — | — | — | — | ✓ |
| Distributed lock | — | — | — | — | — | — | ✓ |
| Distributed lock timeout | — | — | — | — | — | — | ✓ |
| Distributed failure recovery | — | — | — | — | — | — | ✓ |

---

## Choosing a Concurrency Mechanism

The mechanism should follow from the concurrency problem that needs to be solved.

```text
What problem do I have?
        │
        ├── Protect shared mutable state
        │       ↓
        │     lock
        │
        ├── Limit concurrent operations
        │       ↓
        │   SemaphoreSlim
        │
        ├── Transfer work between components
        │   with different processing rates
        │       ↓
        │   Channel<T>
        │       ↓
        │   Bounded Channel → Backpressure
        │
        ├── Share finite reusable resources
        │       ↓
        │   Resource Pool
        │       ↓
        │   Acquire → Use → Release
        │
        ├── Acquire multiple exclusive resources
        │       ↓
        │   Consistent Lock Ordering
        │       ↓
        │   Prevent Deadlock
        │
        ├── Control how frequently operations
        │   may access a service
        │       ↓
        │   Rate Limiting
        │       │
        │       ├── Fixed Window
        │       ├── Sliding Window
        │       └── Token Bucket
        │
        └── Coordinate shared state across
            multiple application instances
                ↓
            Distributed Concurrency
```

When a global lock order cannot be guaranteed, bounded acquisition (`Monitor.TryEnter`) with retry, backoff, and jitter can be used as a recovery strategy.

### Distributed Concurrency Strategies

Once coordination crosses application-instance boundaries, the required guarantee determines the strategy:

```text
Distributed Concurrency
        │
        ├── Atomic Database Update
        │       ↓
        │   Database performs the state
        │   transition atomically
        │
        ├── Optimistic Concurrency
        │       ↓
        │   READ → PROCESS → WRITE
        │       ↓
        │   Detect conflict
        │       ↓
        │   Re-read and retry when safe
        │
        └── Distributed Lock
                ↓
            LOCK → READ → PROCESS
                 → UPDATE → UNLOCK
                ↓
            Exclusive execution
```

- **Atomic Database Update:** preferred when the state transition can be   expressed as a short atomic database operation.
- **Optimistic Concurrency:** useful for `READ → PROCESS → WRITE` operations when conflicts can be detected and safely retried.
- **Distributed Lock:** appropriate when the complete operation requires exclusive execution across application instances.

Two additional factors influence the choice:

- **Contention:** frequent conflicts make optimistic retries more expensive.
- **Duration of exclusion:** long operations make holding exclusive locks more expensive.

This is a conceptual guide. The goal is to identify the required guarantee before choosing the coordination mechanism.

---

## First Case: Butcher Shop Simulation

The first deep dive models a butcher shop where multiple customers may arrive concurrently but only a limited number of butchers are available to serve them.

Although the scenario is intentionally simple, it introduces an important concurrency pattern:

> Multiple independent operations compete for a limited number of resources.

The exercise explores how concurrent customers can wait efficiently for an available butcher without exceeding the shop's service capacity.

The implementation uses .NET asynchronous programming and SemaphoreSlim as the initial synchronization mechanism.

See:

[butcher shop simulation](./01-butcher-shop-simulation.md)

---

## Second Case: Bank Account Race Condition

The second deep dive explores what happens when multiple concurrent operations read and modify the same shared state.

The scenario models two withdrawals competing against the same bank account balance. Although each withdrawal may be individually valid, concurrent execution can allow both operations to validate against the same previous balance and produce an inconsistent result.

The exercise introduces a different concurrency problem:

> Multiple concurrent operations perform a read, validate, and modify sequence over the same mutable state.

The initial implementation intentionally contains no synchronization, making it possible to observe the race condition before introducing a solution.

The exercise progressively explores:

- Shared mutable state.
- Race conditions.
- Execution interleaving.
- Critical sections.
- Atomicity of logical operations.
- Mutual exclusion using `lock`.
- Synchronization scope.

The main objective is to understand how to identify the complete state transition that must remain consistent, rather than simply protecting an individual assignment.

See:

[`02-bank-account-race-condition.md`](./02-bank-account-race-condition.md)

---

## Third Case: Producer-Consumer Problem

The third deep dive explores how independent components coordinate when one produces work and another consumes it at a different rate.

The exercise starts with a shared `Queue<WorkItem>` to expose concurrent access, polling, missing completion signaling, and unbounded accumulation. It then introduces an unbounded `Channel<T>` for asynchronous producer-consumer coordination and finally a bounded channel to observe backpressure when the consumer cannot keep up.

See:

[`03-producer-consumer-problem.md`](./03-producer-consumer.md)

---

## Fourth Case: Delivery Vehicle Resource Pool

The fourth deep dive explores how multiple concurrent operations share a finite set of concrete, reusable resources.

The scenario models deliveries competing for a small pool of vehicles. It starts with an unsafe shared collection, exposes a race condition during acquisition, and then separates two responsibilities: protecting the pool state and waiting asynchronously for resource availability.

The exercise progressively explores:

- Resource acquisition and release.
- Shared pool state and critical sections.
- Asynchronous waiting with `SemaphoreSlim`.
- Concrete resource assignment.
- Resource leaks and guaranteed cleanup with `try/finally`.
- Acquisition timeout and cancellation.
- Consistency between semaphore permits and available resources.

See:

[`04-delivery-vehicle-resource-pool.md`](./04-resource-pool.md)

---

## Fifth Case: Bank Transfer Deadlock

The fifth deep dive explores how concurrent operations can become permanently blocked when they need exclusive access to multiple shared resources.

The scenario models two simultaneous bank transfers between the same accounts in opposite directions. It starts with nested locks acquired according to the business direction of each transfer, reproduces a deadlock, and then explores different strategies for preventing or recovering from conflicting lock acquisition.

The exercise progressively explores:

- Nested locks and circular wait.
- The four conditions required for a deadlock.
- Separation between business direction and lock acquisition order.
- Deadlock prevention through consistent lock ordering.
- Bounded lock acquisition with `Monitor.TryEnter`.
- Retry after failed lock acquisition.
- Livelock caused by synchronized retries.
- Progressive backoff to reduce repeated contention.
- Jitter to break retry symmetry and improve progress.

See:

[05-bank-transfer-deadlock.md](./05-deadlock.md)

---

## Sixth Case: Rate-Limited Services

The sixth deep dive explores how to control how frequently concurrent operations may access a limited external service.

The exercise distinguishes concurrency limits from rate limits and progressively explores Fixed Window, Sliding Window, Token Bucket, retry, and safety margins.

See:

[`06-rate-limited-services.md`](./06-rate-limited-services.md)

---

## Seventh Case: Distributed Inventory

The seventh deep dive extends concurrency reasoning beyond a single process.

The exercise starts with an unsafe inventory update and progressively explores shared synchronization boundaries, atomic database updates, optimistic concurrency and retry, PostgreSQL advisory locks, timeout, failure recovery, and session-level lock lifecycle.

See:

[`07-distributed-inventory.md`](./07-distributed-inventory.md)

---

## Repository Structure

Documentation and executable implementations are intentionally separated:

```text
docs/
└── deep-dives/
    └── concurrency/
        ├── README.md
        ├── 01-butcher-shop-simulation.md
        ├── 02-bank-account-race-condition.md
        ├── 03-producer-consumer-problem.md
        ├── 04-delivery-vehicle-resource-pool.md
        ├── 05-deadlock.md
        ├── 06-rate-limited-services.md
        └── 07-distributed-inventory.md        

src/
└── Concurrency/
    ├── ButcherShopSimulation/
    ├── BankAccountRaceConditionSimulation/
    ├── ProducerConsumerSimulation/
    ├── DeliveryVehiclePoolSimulation/
    ├── BankTransferDeadlockSimulation/
    ├── RateLimitedServiceSimulation/
    └── DistributedInventorySimulation/    
```

The documentation focuses on the problem, reasoning, trade-offs, and lessons learned.

The source code provides executable experiments that make the described concurrency behavior observable.

---

## Guiding Principle

Concurrency mechanisms are implementation tools.

The primary skill developed in this section is the ability to recognize the underlying concurrency problem and determine what guarantees the system actually requires before selecting a synchronization mechanism.
