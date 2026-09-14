# Delivery Vehicle Resource Pool

## 1. Problem Context

A resource pool appears when multiple concurrent operations need temporary access to a finite set of reusable resources.

In this exercise, several **Delivery** operations compete for a small number of concrete **Vehicle** instances.

```text
Delivery 1 ──┐
Delivery 2 ──┼──► Vehicle Pool ──► [Van #1][Van #2][Van #3]
Delivery 3 ──┤
Delivery 4 ──┘
```

The central question is:

> How do we safely manage a finite set of reusable resources among multiple concurrent operations?

Unlike a simple concurrency limit, each successful acquisition must return a specific resource that is later returned to the pool.

The exercise evolves through these concerns:

```text
Naive shared pool
      ↓
Safe pool state
      ↓
Asynchronous resource waiting
      ↓
Reliable resource release
      ↓
Acquisition timeout
      ↓
Pool state consistency
```

---

## 2. Naive Resource Pool

The first implementation stores the available vehicles in a shared:

```csharp
List<Vehicle>
```

Acquisition performs three logical steps:

```text
CHECK availability
        ↓
SELECT vehicle
        ↓
REMOVE vehicle
```

A simplified implementation is:

```csharp
public Vehicle? Acquire()
{
    if (_availableVehicles.Count == 0)
        return null;

    var vehicle = _availableVehicles[0];

    _availableVehicles.RemoveAt(0);

    return vehicle;
}
```

Multiple deliveries execute concurrently against the same pool.

### Race condition during acquisition

The availability check and removal are not a single atomic operation.

Several deliveries can observe the same previous state:

```text
Delivery 1 ── CHECK ───────────── REMOVE
Delivery 2 ───── CHECK ────────── REMOVE
Delivery 3 ───────── CHECK ────── REMOVE
Delivery 4 ───────────── CHECK ── REMOVE
```

To make the race observable, the simulation intentionally introduced a delay between checking availability and removing a vehicle.

With three vehicles, four or more deliveries could all observe that the pool was non-empty before any of them completed the removal. Once the first three vehicles were removed, another delivery could still attempt:

```csharp
_availableVehicles[0]
```

against an empty list.

The experiment reproduced the resulting invalid access.

The important observation is that the critical operation is not only `RemoveAt(0)`. The complete state transition is:

```text
CHECK → SELECT → REMOVE
```

---

## 3. Protecting the Pool State

The first correction protects the shared collection with a common `lock`.

```csharp
private readonly object _sync = new();

public Vehicle? Acquire()
{
    lock (_sync)
    {
        if (_availableVehicles.Count == 0)
            return null;

        var vehicle = _availableVehicles[0];

        _availableVehicles.RemoveAt(0);

        return vehicle;
    }
}

public void Release(Vehicle vehicle)
{
    lock (_sync)
    {
        _availableVehicles.Add(vehicle);
    }
}
```

Now only one operation at a time can execute the acquisition transition over the collection:

```text
Delivery A ──► lock ──► CHECK → SELECT → REMOVE ──► unlock
Delivery B ─────────────── waits ─────────────────►
```

`Release()` uses the same synchronization strategy because it modifies the same shared state.

This removes the race condition, but it exposes a different problem.

---

## 4. Losing the Opportunity to Acquire a Vehicle

With three vehicles and ten deliveries, the first three can acquire a vehicle:

```text
Delivery 1 → Van #1
Delivery 2 → Van #2
Delivery 3 → Van #3
```

The remaining deliveries find an empty pool:

```text
Delivery 4 → no vehicle → returns
Delivery 5 → no vehicle → returns
...
```

Later, the first deliveries return their vehicles:

```text
Delivery 1 → Release Van #1
Delivery 2 → Release Van #2
Delivery 3 → Release Van #3
```

However, the deliveries that previously failed have already lost their opportunity.

Protecting the collection therefore solves:

> Safe concurrent access to the pool state.

It does not solve:

> Waiting until a temporarily unavailable resource is returned.

For this scenario, an empty pool should cause a delivery to wait rather than fail immediately.

---

## 5. Asynchronous Resource Waiting with SemaphoreSlim

The pool introduces a `SemaphoreSlim` whose count represents the number of vehicles currently available.

With three vehicles:

```text
Available vehicles = 3
Semaphore permits  = 3
```

Acquisition begins with:

```csharp
await _availability.WaitAsync(cancellationToken);
```

When all vehicles are in use, the semaphore count reaches zero.

Additional deliveries wait asynchronously:

```text
Delivery 1 → Van #1
Delivery 2 → Van #2
Delivery 3 → Van #3

Delivery 4 → WaitAsync()
Delivery 5 → WaitAsync()
Delivery 6 → WaitAsync()
```

When a vehicle is returned:

```csharp
_availability.Release();
```

one pending acquisition can continue.

The pool now combines two responsibilities:

```text
SemaphoreSlim
      ↓
coordinates availability

List<Vehicle> + lock
      ↓
stores and protects the concrete resources
```

The semaphore answers:

> Is a resource available for acquisition?

The collection answers:

> Which concrete resource is available?

A representative implementation is:

```csharp
public async Task<Vehicle> AcquireAsync(
    CancellationToken cancellationToken)
{
    await _availability.WaitAsync(cancellationToken);

    lock (_sync)
    {
        var vehicle = _availableVehicles[0];

        _availableVehicles.RemoveAt(0);

        return vehicle;
    }
}

public void Release(Vehicle vehicle)
{
    lock (_sync)
    {
        _availableVehicles.Add(vehicle);
    }

    _availability.Release();
}
```

The release order is intentional:

```text
add concrete vehicle to pool
            ↓
announce availability
```

The pool must not announce availability before the resource has actually been returned.

### Observed behavior

With three vehicles and ten deliveries, the execution showed the remaining deliveries waiting rather than disappearing.

As vehicles were returned, pending deliveries acquired them and continued.

The exact continuation order was not sequential. For example, deliveries `4`, `6`, and `5` resumed in that order.

This reinforces that concurrent task scheduling should not be used as an ordering mechanism.

---

## 6. Resource Lifecycle and Resource Leaks

A pooled resource has a lifecycle:

```text
Acquire
   ↓
Use
   ↓
Release
```

A new problem appears if the work fails after acquisition but before release:

```text
Delivery
   ↓
Acquire Van #2
   ↓
processing
   ↓
EXCEPTION
```

If `Van #2` is never returned, the resource still exists but is no longer available through the pool.

This is a **resource leak**.

```text
Initial effective capacity: 3

one leaked vehicle
        ↓
effective capacity: 2

another leaked vehicle
        ↓
effective capacity: 1
```

Repeated leaks can eventually exhaust the pool.

The release therefore has to be guaranteed even when the delivery fails:

```csharp
var vehicle = await pool.AcquireAsync(cancellationToken);

try
{
    await ExecuteDeliveryAsync(vehicle, cancellationToken);
}
finally
{
    pool.Release(vehicle);
}
```

The important guarantee is:

> Once a resource has been successfully acquired, its release must occur even if the operation using it fails.

---

## 7. Pool Exhaustion and Acquisition Timeout

Waiting asynchronously avoids losing deliveries when the pool is temporarily empty, but waiting indefinitely is not always desirable.

The pool can apply an acquisition timeout:

```csharp
var acquired = await _availability.WaitAsync(
    timeoutMilliseconds,
    cancellationToken);

if (!acquired)
    return null;
```

The acquisition can now finish in three ways:

```text
AcquireAsync
     │
     ├── resource becomes available
     │        ↓
     │     return Vehicle
     │
     ├── timeout expires
     │        ↓
     │     return null
     │
     └── cancellation requested
              ↓
       OperationCanceledException
```

Timeout and cancellation represent different decisions.

A timeout means:

> The operation waited too long for this resource.

Cancellation means:

> The operation itself should no longer continue.

For example, if deliveries use vehicles for `5000 ms` but acquisition waits only `2000 ms`, pending deliveries can time out before a vehicle is returned.

This is not necessarily a pool failure. It is the result of temporary pool exhaustion combined with the caller's waiting policy.

---

## 8. Keeping Semaphore and Resource State Consistent

The semaphore count and the available-resource collection represent related views of the pool state.

Conceptually:

```text
Semaphore permits
        ↕
available vehicles
```

After `WaitAsync()` succeeds, one permit has already been consumed.

If an exception occurs before the vehicle is successfully removed from the collection, the permit must be restored:

```csharp
public async Task<Vehicle?> AcquireAsync(
    int timeoutMilliseconds,
    CancellationToken cancellationToken)
{
    var acquired = await _availability.WaitAsync(
        timeoutMilliseconds,
        cancellationToken);

    if (!acquired)
        return null;

    try
    {
        lock (_sync)
        {
            var vehicle = _availableVehicles[0];

            _availableVehicles.RemoveAt(0);

            return vehicle;
        }
    }
    catch
    {
        _availability.Release();
        throw;
    }
}
```

Otherwise the pool could become inconsistent:

```text
Semaphore count          = 2
AvailableVehicles.Count  = 3
```

The central invariant is:

> Every consumed permit must correspond to a resource that was actually acquired.

And during release:

> Every semaphore release must correspond to a resource that was actually returned.

This is why the normal release sequence remains:

```text
add resource
    ↓
Semaphore.Release()
```

---

## 9. Final Resource Pool Model

The completed model combines several guarantees:

```text
Delivery
   │
   ▼
AcquireAsync
   │
   ├── SemaphoreSlim
   │      waits for availability
   │
   └── lock
          protects selection/removal
             │
             ▼
          Vehicle
             │
             ▼
          try/use
             │
             ▼
          finally
             │
             ▼
          Release
             │
             ├── return Vehicle
             └── Semaphore.Release()
```

Each mechanism has a distinct responsibility.

| Concern | Mechanism |
|---|---|
| Protect the shared vehicle collection | `lock` |
| Wait until capacity becomes available | `SemaphoreSlim.WaitAsync()` |
| Represent the concrete reusable resources | `List<Vehicle>` |
| Guarantee resource return after acquisition | `try/finally` |
| Bound how long acquisition may wait | `WaitAsync(timeout, cancellationToken)` |
| Preserve internal pool consistency | Restore permits when acquisition cannot complete |

---

## 10. Conclusion

The main lesson of the exercise is that a resource pool is more than a concurrency limit.

A pool must coordinate access to a finite set of **concrete reusable resources** and preserve their lifecycle:

```text
Acquire → Use → Release
```

The exercise progressively exposed the responsibilities required to do that safely:

1. protect shared pool state from race conditions;
2. wait asynchronously when all resources are temporarily in use;
3. associate availability with concrete resources;
4. guarantee release when work fails;
5. avoid indefinite acquisition through timeout and cancellation;
6. keep the availability mechanism consistent with the actual resources in the pool.

`SemaphoreSlim` coordinates availability, but it does not replace the resource collection. The pool needs both the logical availability count and the concrete resources, with synchronization that keeps those two representations consistent.
