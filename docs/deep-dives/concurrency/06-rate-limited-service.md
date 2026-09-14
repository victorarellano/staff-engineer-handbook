# 06 - Rate-Limited Services

## 1. Problem

Concurrent applications can generate requests faster than an external service is willing to accept them.

The central question of this exercise is:

> How do we control how frequently concurrent operations may access a limited external service?

The scenario models an external payment API that accepts at most:

```text
5 requests / second
```

The application generates 20 concurrent payment requests and progressively evaluates different strategies for controlling the outgoing traffic.

---

## 2. Concurrency Limit vs Rate Limit

These are related but different problems.

```text
Concurrency limit
"How many operations may run at the same time?"

Rate limit
"How many operations may occur during a period of time?"
```

A concurrency limiter can reduce simultaneous work without controlling how frequently new requests are started.

A rate limiter explicitly introduces a time-based policy.

---

## 3. Simulation Model

```text
Application
    ├── Request 01
    ├── Request 02
    ├── ...
    └── Request 20
            │
            ▼
    Client-side strategy
            │
            ▼
     External Payment API
        limit: 5 req/s
```

The simulated `PaymentApi` maintains its own rate window and returns an equivalent of:

```text
429 Too Many Requests
```

when more than five requests arrive inside its current one-second window.

The server-side window is intentionally independent from the client-side limiter. This exposes an important real-world constraint:

> A client can regulate its own traffic, but it does not control the internal timing or state of the external server.

---

## 4. Scenario 1 - Naive Concurrent Requests

The first version sends all requests concurrently without any client-side control.

```csharp
var tasks = requests
    .Select(request =>
        _paymentService.ProcessAsync(
            request,
            cancellationToken))
    .ToArray();

var results = await Task.WhenAll(tasks);
```

Observed result:

```text
Accepted: 5
Rejected: 15
```

The application produces requests much faster than the external API can accept them.

---

## 5. Scenario 2 - Concurrency Limiting with SemaphoreSlim

The second version limits the number of simultaneous operations:

```csharp
await _concurrencyLimit.WaitAsync(cancellationToken);

try
{
    return await _paymentApi.ProcessAsync(
        request,
        cancellationToken);
}
finally
{
    _concurrencyLimit.Release();
}
```

With a semaphore capacity of five:

```text
SemaphoreSlim(5)
```

only five calls may execute concurrently.

However, the permit is released as soon as a request finishes.

If the API responds quickly, another request may start immediately inside the same server window.

Observed result:

```text
Accepted: 5
Rejected: 15
```

The experiment demonstrates:

> Limiting concurrency does not necessarily limit request frequency.

---

## 6. Scenario 3 - Fixed Window Rate Limiting

The next strategy introduces a time-based policy using `FixedWindowRateLimiter`.

Conceptually:

```text
|--- Window 1 ---|--- Window 2 ---|--- Window 3 ---|
      5 max             5 max             5 max
```

The client allows at most five requests inside each of its one-second windows.

A queue is used so requests exceeding the current capacity wait for a future window instead of being immediately rejected by the client limiter.

Observed result:

```text
Accepted: 15
Rejected: 5
```

This is a clear improvement over the naive and concurrency-limited versions.

### Independent Window Boundaries

The client and server maintain separate windows:

```text
Client:
|------|------|------|

Server:
   |------|------|------|
```

Their boundaries are not synchronized.

Therefore, the client may release a new batch while the server still considers the previous server-side window active.

The result is that the client can respect:

```text
5 requests per client window
```

and still receive:

```text
429 Too Many Requests
```

from the server.

This experiment demonstrates:

> A client-side fixed window cannot reproduce the internal window boundaries of an independent external service.

---

## 7. Scenario 4 - Sliding Window Rate Limiting

The next version uses `SlidingWindowRateLimiter`.

Instead of resetting the complete counter at one rigid boundary, the window is divided into smaller segments.

Example:

```text
1 second

|100ms|100ms|100ms|100ms|...|
```

Older segments progressively leave the active window.

This reduces some of the boundary effects associated with fixed windows.

However, with the workload used in this exercise, Fixed Window and Sliding Window behaved almost identically.

Observed result:

```text
Accepted: 15
Rejected: 5
```

This does not mean both algorithms are equivalent.

It means the simulated workload did not expose a meaningful practical difference between them.

The test starts 20 requests almost simultaneously. Both limiters therefore consume their initial capacity immediately and queue the remaining work.

The experiment also reinforces another point:

> Sliding Window improves how the client measures its own recent traffic, but it still cannot synchronize itself with an independent server window.

---

## 8. Scenario 5 - Token Bucket

The next strategy changes the model.

Instead of counting requests inside windows, Token Bucket manages a number of tokens.

```text
Request
   │
   ▼
Consume token
   │
   ▼
External API
```

A request may proceed only when a token is available.

Tokens are replenished over time.

For a theoretical rate of five requests per second:

```text
1000 ms / 5 = 200 ms
```

Using:

```text
TokenLimit = 1
TokensPerPeriod = 1
ReplenishmentPeriod = 200 ms
```

prevents an initial burst and creates an approximately paced flow:

```text
█   █   █   █   █   █   █
```

rather than:

```text
█████          █████
```

Observed result:

```text
Accepted: 18
Rejected: 2
```

The remaining rejections occurred because the client was operating very close to the theoretical server limit.

Small differences in timer scheduling and independent server timing were enough for some requests to reach the server before its current window had expired.

---

## 9. Safety Margin

To avoid operating exactly at the published limit, the client introduces a configurable safety margin.

Example:

```text
External server limit = 5 req/s
Safety margin         = 80%
Effective client rate = 4 req/s
```

The effective rate is calculated from configuration:

```csharp
var effectiveRate =
    settings.ExternalLimitPerSecond *
    settings.SafetyMargin;
```

The token replenishment period is then derived from that effective rate:

```csharp
var replenishmentPeriod =
    TimeSpan.FromMilliseconds(
        1000.0 / effectiveRate);
```

With:

```text
ExternalLimitPerSecond = 5
SafetyMargin = 0.8
```

the client sends approximately one request every:

```text
250 ms
```

Observed result:

```text
Accepted: 20
Rejected: 0
```

The safety margin should not be interpreted as a guarantee that the server will never return `429`.

It is a preventive strategy that gives the client operational headroom.

---

## 10. Retry

Even with preventive rate limiting, the external service remains the final authority.

A production client must still be prepared for recoverable server responses such as:

```text
429 Too Many Requests
```

The exercise therefore keeps a retry mechanism.

Conceptually:

```text
Token Bucket
     │
     ▼
Send request
     │
     ▼
Server
 ┌───┴─────────────┐
 │                 │
OK              recoverable error
 │                 │
 ▼                 ▼
Done              Wait
                    │
                    ▼
                  Retry
                    │
                    ▼
               Rate Limiter
```

An important rule is:

> Every retry must pass through the rate limiter again.

A retry is another request and therefore also consumes service capacity.

Retry and safety margin solve different problems:

```text
Safety margin
→ reduces the probability of exceeding the server limit.

Retry
→ recovers when a recoverable rejection still occurs.
```

---

## 11. Comparison of Strategies

| Strategy | What it controls | Result in this exercise | Main lesson |
|---|---|---:|---|
| Naive concurrent requests | Nothing | 5 / 20 accepted | Concurrent bursts easily overwhelm a limited API |
| `SemaphoreSlim` | Simultaneous operations | 5 / 20 accepted | Concurrency limiting is not rate limiting |
| Fixed Window | Requests per local time window | 15 / 20 accepted | Local windows reduce traffic but may not align with server windows |
| Sliding Window | Requests over a moving local window | 15 / 20 accepted | Better local accounting does not synchronize with the server |
| Token Bucket | Token-based request pacing | 18 / 20 accepted | Explicit pacing reduces burstiness |
| Token Bucket + 80% safety margin | Conservative request pacing | 20 / 20 accepted | Headroom avoids operating directly on the server boundary |
| Retry | Recovery after recoverable failure | Not triggered in final run | Prevention and recovery are complementary |

> Results belong to this simulation and should not be interpreted as universal performance rankings of the algorithms.

---

## 12. Producer-Consumer vs Rate Limiting

Producer-Consumer and Rate Limiting can both regulate flow, but they solve different problems.

A bounded producer-consumer pipeline:

```text
Producer
   │
   ▼
Bounded Channel
   │
   ▼
Consumer
```

mainly answers:

> What happens when work is produced faster than it can be consumed?

Its key mechanisms are:

- buffering,
- asynchronous coordination,
- bounded capacity,
- backpressure.

Rate limiting instead answers:

> At what frequency is this operation allowed to reach an external resource?

A consumer may process work very quickly while a rate limiter deliberately slows calls to an external API.

Both mechanisms can coexist:

```text
Producer
   │
   ▼
Bounded Channel
   │
   ▼
Consumer
   │
   ▼
Token Bucket
   │
   ▼
External API
```

In that architecture:

```text
Bounded Channel
→ controls accumulation and backpressure.

Token Bucket
→ controls request frequency.
```

---

## 13. Final Mental Model

```text
What problem am I solving?
        │
        ├── Too many operations at the same time
        │         ↓
        │   Concurrency Limiting
        │     SemaphoreSlim
        │
        ├── Too much work accumulating
        │         ↓
        │   Producer-Consumer
        │    Bounded Channel
        │     Backpressure
        │
        └── Too many requests over time
                  ↓
             Rate Limiting
                  │
          ┌───────┼───────────┐
          │       │           │
       Fixed   Sliding      Token
       Window   Window      Bucket
                              │
                              ▼
                           Pacing
                              │
                              ▼
                       Safety Margin
                              │
                              ▼
                            Retry
```

The most important conclusion of the exercise is:

> Client-side rate limiting is a traffic-control policy, not synchronization with the external server.

The client controls what it sends.

The server decides what it accepts.

## 14. Real-World Scenario — Protecting an External API Without Losing Work

Suppose a client application can generate work faster than an external API is allowed to receive it.

```text
Client capacity:    100 requests/min
Provider quota:       5 requests/min
```

Sending requests directly to the provider may cause requests to be rejected once the agreed quota is exceeded.

If the generated work must not be lost, the application can combine the Producer–Consumer pattern with Rate Limiting:

```text
        Client workload
              │
              ▼
           Producer
              │
              ▼
      ┌────────────────┐
      │ Queue / Channel│
      │ pending work   │
      └────────────────┘
              │
              ▼
           Consumer
              │
              ▼
       Token Bucket
        Rate Limiter
              │
              │ ≤ provider quota
              ▼
        External API
```

Each mechanism has a separate responsibility:

|Component|Responsibility|
|---------|--------------|
|Producer–Consumer|	Decouples the rate at which work is generated from the rate at which it can be processed|
|Queue / Channel|	Holds pending work instead of immediately sending it to the provider|
|Consumer|	Retrieves pending work for processing|
|Token Bucket|	Controls when requests may be sent according to the provider's quota|
|Retry|	Handles recoverable failures that still occur despite preventive rate limiting|

The resulting flow is:

```text
Work may arrive quickly
        ↓
Queue absorbs the difference
        ↓
Consumer retrieves the work
        ↓
Token Bucket controls request pacing
        ↓
Provider receives traffic within the expected quota
```

>Producer–Consumer manages the difference between workload production and consumption capacity, while Rate Limiting enforces the temporal consumption policy of the external service.