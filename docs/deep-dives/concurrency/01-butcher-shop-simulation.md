# Butcher Shop Simulation

## 1. Problem Context

A butcher shop has a limited service capacity: several customers may arrive and wait at the same time, but only a fixed number can be served concurrently.

The simulation models a working day with the following rules:

-   The shop has opening and closing hours.
-   There is a lunch break during which arriving customers are rejected.
-   Customers arriving outside working hours are rejected.
-   Customers arrive progressively during the simulation.
-   Service duration varies between configured minimum and maximum values.
-   The number of butchers defines the maximum service capacity.
-   Waiting, service, and rejection statistics are collected.

The scenario is intentionally simple. Its purpose is to make concurrency behavior visible through a familiar real-world problem.

------------------------------------------------------------------------

## 2. From the Business Problem to the Concurrency Problem

The business rule is simple:

> The shop can have many customers waiting, but it can only serve as many customers simultaneously as its available service capacity allows.

This creates a concurrency problem:

``` text
Many independent customers
          +
Limited service capacity
          =
Need to coordinate concurrent access
```

For example, if the shop has two butchers:

``` text
Customer 1 ──► being served
Customer 2 ──► being served
Customer 3 ──► must wait
Customer 4 ──► must wait
```

The system therefore needs to guarantee that no more than two customer service operations execute simultaneously.

This requirement is different from deciding which customer should be served first. Capacity control and queue ordering are separate concerns.

------------------------------------------------------------------------

## 3. Main Components

The implementation separates orchestration, service coordination, customer service, and statistics.

``` text
Program
   │
   ▼
Generic Host
   │
   ▼
SimulationWorker
   │
   ▼
Simulation
   │
   ▼
Customer Service Coordinator
   │
   ├── Service Capacity
   │
   └── Butchers
          │
          ▼
       Customer

Statistics
```

### `SimulationWorker`

The application runs as a .NET `BackgroundService`.

The worker starts the simulation, waits for it to finish, and prints the final statistics.

### `Simulation`

`Simulation` orchestrates the simulated working day.

For every ticket it:

1.  Calculates the simulated arrival time.
2.  Checks whether the customer arrives during lunch.
3.  Checks whether the customer arrives outside working hours.
4.  Rejects invalid arrivals.
5.  Creates a `Customer` for accepted arrivals.
6.  starts a service operation for that customer.
7.  waits for a configurable interval before generating the next arrival.

The resulting service tasks are collected and finally awaited with:

``` csharp
await Task.WhenAll(tasks);
```

This means that several customer operations can exist concurrently.

------------------------------------------------------------------------

## 4. The Important Distinction: Operation, Task, and Thread

Understanding this distinction is fundamental to the exercise.

A customer service operation may exist for a relatively long time:

``` text
Customer arrives
      │
      ▼
Waits for capacity
      │
      ▼
Receives service
      │
      ▼
Finishes
```

That does **not** mean that one .NET thread must remain dedicated to that customer for the entire operation.

A `Task` represents asynchronous work and its eventual completion.

A thread is an execution resource used by .NET to execute code.

Therefore:

``` text
Task / asynchronous operation
        ≠
Dedicated thread
```

An asynchronous operation can be waiting without keeping a thread blocked for the entire waiting period.

This distinction becomes visible when the customer waits for service capacity.

------------------------------------------------------------------------

## 5. Representing Limited Service Capacity

The implementation uses `SemaphoreSlim` to represent the number of customer service operations that may execute concurrently.

Conceptually, if there are three butchers:

``` text
Service Capacity = 3 permits
```

The semaphore is initialized with that capacity:

``` csharp
new SemaphoreSlim(butchersCount, butchersCount);
```

Each customer must acquire one permit before entering service.

``` text
Capacity = 3

Customer 1 acquires permit → capacity = 2
Customer 2 acquires permit → capacity = 1
Customer 3 acquires permit → capacity = 0

Customer 4 arrives
        │
        ▼
No capacity available
        │
        ▼
Must wait
```

When a customer finishes, the permit is returned and another waiting
operation may continue.

------------------------------------------------------------------------

## 6. Asynchronous Waiting with `WaitAsync`

The customer waits for capacity using:

``` csharp
await _serviceCapacity.WaitAsync(cancellationToken);
```

If capacity is available, `WaitAsync` can complete and execution
continues.

If capacity is not available, the customer operation cannot continue
beyond that line.

For example:

``` csharp
await _serviceCapacity.WaitAsync(cancellationToken);

// This code does NOT execute until capacity is obtained.
try
{
    await butcher.AttendAsync(...);
}
finally
{
    _serviceCapacity.Release();
}
```

The important point is:

> The customer operation waits, but the thread that was executing it does not need to remain blocked while that wait occurs.

Suppose Customer 3 arrives when all service capacity is occupied.

``` text
Thread 7
   │
   ▼
ServeCustomerAsync(Customer 3)
   │
   ▼
await _serviceCapacity.WaitAsync(...)
   │
   │ no capacity available
   ▼
ServeCustomerAsync is suspended here
   │
   └──────────────► Thread 7 becomes available
                    to execute other work
```

The code after the `await` is **not** executed.

What becomes available is the thread, not the remainder of the customer operation.

Later, another customer finishes:

``` csharp
_serviceCapacity.Release();
```

The semaphore can then satisfy a pending wait.

``` text
Customer 1 finishes
        │
        ▼
     Release()
        │
        ▼
Capacity becomes available
        │
        ▼
Customer 3's wait can complete
        │
        ▼
.NET schedules the continuation
        │
        ▼
A thread executes the code
after the await
```

The thread executing the continuation does not necessarily have to be the same thread that originally reached the `await`.

Conceptually:

``` text
TIME ───────────────────────────────────────────────►

Thread 7:
    Customer 3
        │
        ▼
    WaitAsync()
        │
        X operation suspended
        │
        └──── thread available


                       Customer 1 finishes
                              │
                           Release()
                              │
                              ▼
                    wait can complete


Thread Pool:
                              │
                              ▼
                     continuation scheduled
                              │
                              ▼
                       available thread
                              │
                              ▼
                     continue Customer 3
                     after the await
```

This is what is meant by **asynchronous waiting** in this exercise.

------------------------------------------------------------------------

## 7. `Wait` versus `WaitAsync`

The difference is not:

``` text
Wait      → waits
WaitAsync → does not wait
```

Both operations may need to wait.

The relevant difference is what happens to the executing thread while
the operation cannot continue.

### Blocking wait

Conceptually:

``` csharp
_serviceCapacity.Wait(cancellationToken);
```

If capacity is unavailable:

``` text
Customer operation waits
        +
Executing thread remains blocked
```

The thread cannot be used for other work until the wait finishes.

### Asynchronous wait

With:

``` csharp
await _serviceCapacity.WaitAsync(cancellationToken);
```

if capacity is unavailable:

``` text
Customer operation waits
        +
Method is suspended
        +
Executing thread becomes available
```

When capacity becomes available, .NET schedules the continuation of the
suspended operation.

Therefore:

``` text
Wait()
────────────────────────────
Operation waits
Thread waits


await WaitAsync()
────────────────────────────
Operation waits
Thread does not need to remain blocked
```

This is one of the central lessons of the exercise.

------------------------------------------------------------------------

## 8. Acquiring and Releasing Capacity

Once a customer obtains capacity, service can begin.

The coordination structure is:

``` csharp
await _serviceCapacity.WaitAsync(cancellationToken);

try
{
    await butcher.AttendAsync(
        customer,
        statistics,
        serviceMin,
        serviceMax,
        cancellationToken);
}
finally
{
    _serviceCapacity.Release();
}
```

The lifecycle is:

``` text
Customer arrives
      │
      ▼
Wait for capacity
      │
      ▼
Acquire capacity
      │
      ▼
Receive service
      │
      ▼
Release capacity
```

Using `finally` is important.

Once capacity has been acquired, it must be released even if service fails or cancellation occurs after acquisition.

This expresses a general principle:

> Acquiring a limited resource creates an obligation to release it.

------------------------------------------------------------------------

## 9. Measuring Contention

Before waiting for service capacity, the implementation starts a `Stopwatch`.

``` csharp
var stopwatch = Stopwatch.StartNew();

await _serviceCapacity.WaitAsync(cancellationToken);
```

When the wait completes, the elapsed time indicates how long the customer waited before service capacity became available.

The current implementation records the wait when it exceeds a small threshold.

This makes contention observable.

If customers arrive faster than the system can serve them:

``` text
Arrival rate increases
        │
        ▼
More customers compete for capacity
        │
        ▼
More operations wait
        │
        ▼
Waiting time increases
```

Concurrency is therefore not only visible in the code; its effects can also be measured.

------------------------------------------------------------------------

## 10. Simulating Customer Service

`Butcher.AttendAsync` represents the actual service operation.

A random service duration is selected between configured limits:

``` csharp
var serviceTime =
    TimeSpan.FromMinutes(
        new Random().Next(minMinutes, maxMinutes + 1));
```

Service is simulated asynchronously:

``` csharp
await Task.Delay(serviceTime, cancellationToken);
```

Again, the operation is waiting for time to elapse, but it does not
require a thread to remain blocked for the duration of that delay.

After service finishes, the total customer time in the system is
calculated from the recorded arrival time.

------------------------------------------------------------------------

## 11. A Second Concurrency Problem: Shared Statistics

Limiting service capacity is not the only concurrency problem in the
simulation.

Several customer operations can update the same `Statistics` object.

This introduces **shared mutable state**.

The implementation uses different mechanisms depending on the required
guarantee.

### Protecting collections with `lock`

Waiting and service times are stored in shared lists.

Updates are protected using:

``` csharp
lock (_waitTimes)
{
    _waitTimes.Add(waitTime);
}
```

and:

``` csharp
lock (_serviceTimes)
{
    _serviceTimes.Add(serviceTime);
}
```

Here the requirement is mutual exclusion while modifying shared mutable state.

### Atomic counters with `Interlocked`

Counters use:

``` csharp
Interlocked.Increment(ref _servedCustomers);
```

and:

``` csharp
Interlocked.Increment(ref _rejectedCustomers);
```

The required operation is only an atomic increment, so a larger critical section is unnecessary.

This gives three different mechanisms in the same exercise:

  Mechanism         Required guarantee
  ----------------- ---------------------------------------------
  `SemaphoreSlim`   Limit concurrent access to service capacity
  `lock`            Protect mutable shared state
  `Interlocked`     Perform a simple atomic update

The mechanism should follow the problem, not the other way around.

------------------------------------------------------------------------

## 12. Cancellation

`CancellationToken` is propagated through the simulation.

It participates in:

``` csharp
await _serviceCapacity.WaitAsync(cancellationToken);
```

customer arrival delays:

``` csharp
await Task.Delay(arrivalDelay, cancellationToken);
```

and customer service:

``` csharp
await Task.Delay(serviceTime, cancellationToken);
```

Therefore, an operation that is waiting does not have to remain pending indefinitely when application shutdown or cancellation is requested.

------------------------------------------------------------------------

## 13. Capacity Control Is Not FIFO Ordering

Customers receive sequential ticket numbers, but the current design should not be interpreted as an explicit FIFO queue.

The semaphore guarantees:

``` text
No more than N customer service operations
execute concurrently.
```

That is a capacity guarantee.

It is different from:

``` text
Customers must always begin service
strictly according to ticket order.
```

That is an ordering guarantee.

Conceptually:

``` text
Who should be served next?
        │
        └── queueing policy

How many can be served?
        │
        └── concurrency policy
```

If strict FIFO becomes a requirement, it should be modeled explicitly rather than assumed from the semaphore.

------------------------------------------------------------------------

## 14. Current Implementation Limitation: Butcher Assignment

The current implementation creates multiple `Butcher` instances
according to `ButchersCount`.

However, after acquiring service capacity, it currently selects:

``` csharp
var butcher = _butchers.First();
```

Therefore, the semaphore correctly models aggregate service capacity, but individual butcher allocation is not currently modeled.

Several concurrent service operations may invoke `AttendAsync` on the same `Butcher` instance.

Conceptually:

``` text
Aggregate service capacity
        │
        └── modeled by SemaphoreSlim

Individual available butcher
        │
        └── not explicitly allocated
```

This is an implementation limitation and also a useful next design problem.

A future version could coordinate actual available butcher instances instead of representing only aggregate capacity.

------------------------------------------------------------------------

## 15. What the Exercise Actually Teaches

The exercise is not primarily about learning the `SemaphoreSlim` API.

It demonstrates how to move from a real-world constraint to a concurrency model.

``` text
Business constraint
        │
        ▼
Limited number of customers
can be served simultaneously
        │
        ▼
Concurrency requirement
        │
        ▼
Coordinate access to
limited service capacity
        │
        ▼
Synchronization strategy
        │
        ▼
SemaphoreSlim
```

It also demonstrates that a single application may contain several independent concurrency concerns:

``` text
Service capacity
      │
      └── SemaphoreSlim

Shared collections
      │
      └── lock

Shared counters
      │
      └── Interlocked

Waiting operations
      │
      └── async / await
```

------------------------------------------------------------------------

## 16. Key Lessons

### 1. Start with the constraint

Do not begin with:

> Where can `SemaphoreSlim` be used?

Begin with:

> What operation must be limited, coordinated, or protected?

------------------------------------------------------------------------

### 2. A Task is not a Thread

An asynchronous operation may remain incomplete without owning a
dedicated thread throughout its lifetime.

------------------------------------------------------------------------

### 3. Asynchronous waiting still means waiting

When:

``` csharp
await _serviceCapacity.WaitAsync(cancellationToken);
```

cannot complete, execution of that method stops at that point.

The code after the `await` does not run.

What is avoided is keeping the executing thread blocked while the
operation waits.

------------------------------------------------------------------------

### 4. `WaitAsync` can improve use of managed execution resources

While the customer operation waits for capacity, the thread that reached
the `await` can become available to the runtime for other work.

When the wait completes, the continuation is scheduled for execution.

------------------------------------------------------------------------

### 5. `async` does not provide synchronization by itself

`async`/`await` controls how asynchronous operations wait and resume.

`SemaphoreSlim` provides the capacity constraint.

They solve different problems.

------------------------------------------------------------------------

### 6. Different concurrency problems require different mechanisms

The same simulation uses:

``` text
SemaphoreSlim → limited capacity
lock          → shared mutable collections
Interlocked   → atomic counters
```

------------------------------------------------------------------------

### 7. Resource acquisition requires guaranteed release

The semaphore permit is released in `finally` so that failures do not permanently reduce available capacity.

------------------------------------------------------------------------

### 8. Capacity and ordering are separate concerns

A semaphore limits how many operations execute concurrently. It should not be treated as the business queue itself.

------------------------------------------------------------------------

## 17. Experiments

The simulation can be modified to make different concurrency behaviors visible.

### Experiment 1 --- Change Service Capacity

Modify:

``` text
ButchersCount
```

Compare waiting times with one, two, and several service slots.

------------------------------------------------------------------------

### Experiment 2 --- Increase Arrival Rate

Reduce:

``` text
ArrivalTimeMinMs
ArrivalTimeMaxMs
```

Observe how more customer operations begin competing for limited
capacity.

------------------------------------------------------------------------

### Experiment 3 --- Increase Service Duration

Increase:

``` text
ServiceTimeMinMinutes
ServiceTimeMaxMinutes
```

Capacity remains occupied longer, increasing contention.

------------------------------------------------------------------------

### Experiment 4 --- Use a Single Service Slot

Configure:

``` text
ButchersCount = 1
```

Customer service becomes effectively serialized.

------------------------------------------------------------------------

### Experiment 5 --- Observe Cancellation While Waiting

Cancel the application while customers are waiting for service capacity.

Observe the effect of the propagated `CancellationToken`.

------------------------------------------------------------------------

### Experiment 6 --- Compare `Wait` and `WaitAsync`

As a controlled learning experiment, compare the conceptual and runtime implications of:

``` csharp
_serviceCapacity.Wait(cancellationToken);
```

with:

``` csharp
await _serviceCapacity.WaitAsync(cancellationToken);
```

The important observation is not whether the customer waits --- it waits in both cases --- but whether a thread must remain blocked during that wait.

------------------------------------------------------------------------

## 18. Possible Evolution

The current solution can be extended progressively to introduce new concurrency problems.

Possible next steps include:

-   Explicit allocation of available `Butcher` instances.
-   A real FIFO customer queue.
-   Producer-consumer coordination.
-   `Channel<T>` as a customer queue.
-   Bounded queues and backpressure.
-   Customers abandoning the queue after a timeout.
-   Queue-length and active-service metrics.
-   Comparison between semaphore-based and worker-based designs.

This allows the simple butcher shop scenario to evolve into a richer
concurrency laboratory.

------------------------------------------------------------------------

## 19. Guiding Principle

The most reusable lesson is not:

``` text
Use SemaphoreSlim when there are butchers.
```

It is:

``` text
Many independent operations
        +
Limited shared capacity
        =
Concurrency coordination is required
```

The engineering process should therefore be:

``` text
Understand the problem
        ↓
Identify concurrent actors
        ↓
Identify shared or limited resources
        ↓
Define the guarantees required
        ↓
Choose the coordination mechanism
        ↓
Implement and observe the behavior
```

The synchronization primitive is the consequence of the reasoning, not the starting point.
