# Producer-Consumer Problem

## 1. Problem Context

A producer-consumer problem appears when one component generates work and another processes it independently.

In this exercise, the **Producer** creates `WorkItem` instances and the **Consumer** processes them. Both execute concurrently, and the producer can generate work faster than the consumer can process it.

```text
             ┌─────────────────┐
Producer ───►│   Pending Work  │───► Consumer
             └─────────────────┘
```

The central question is:

> How do we coordinate components that produce and consume work at different rates?

The exercise preserves three implementations:

```text
Queue<T>
   ↓
Channel<T> Unbounded
   ↓
Channel<T> Bounded
```

---

## 2. Naive Implementation with Queue<T>

The first implementation uses a shared:

```csharp
Queue<WorkItem>
```

Both components access the same instance:

```text
Producer ── Enqueue() ──┐
                        ▼
                 Queue<WorkItem>
                        ▲
Consumer ── Dequeue() ──┘
```

The consumer also checks periodically whether work is available.

This exposes four problems.

### Concurrent access

`Enqueue()` and `Dequeue()` operate concurrently over the same mutable `Queue<WorkItem>`. `Queue<T>` does not provide the synchronization required for concurrent producer-consumer access.

A manual synchronization mechanism could protect access to the queue, but that alone would not solve the remaining coordination problems.

### Polling

When no item exists, the consumer repeatedly checks the queue:

```text
check queue
    ↓
empty
    ↓
wait
    ↓
check again
    ↓
...
```

The consumer has no mechanism to wait specifically for new work to arrive.

### No completion signal

The producer finishes after generating `ItemCount` elements, but the consumer does not know that production has finished:

```text
Producer finishes
       ↓
Consumer processes remaining items
       ↓
Queue becomes empty
       ↓
Consumer continues polling
```

For this naive scenario, external cancellation (`Ctrl+C`) is required.

### Unbounded growth

If the producer remains faster than the consumer, pending work accumulates:

```text
Producer ───► [1][2][3][4][5][6][7][8]... ───► Consumer
```

There is no configured maximum for pending work.

---

## 3. Introducing Channel<T>

The problem requires more than a shared collection. Producer and consumer need a communication mechanism capable of coordinating their different execution rates.

.NET provides `Channel<T>` for asynchronous producer-consumer communication.

```text
Producer
   │
   │ ChannelWriter<T>
   ▼
┌──────────────────┐
│    Channel<T>    │
└──────────────────┘
   │
   │ ChannelReader<T>
   ▼
Consumer
```

The producer writes with:

```csharp
await writer.WriteAsync(item, cancellationToken);
```

The consumer can read asynchronously:

```csharp
await foreach (
    var item in reader.ReadAllAsync(cancellationToken))
{
    await ProcessAsync(item);
}
```

If no item is available, the consumer can wait asynchronously instead of polling.

The producer can also signal that no more work will arrive:

```csharp
writer.TryComplete();
```

The consumer then processes any buffered items and terminates naturally when the channel is empty.

---

## 4. Unbounded Channel

The second implementation uses:

```csharp
Channel.CreateUnbounded<WorkItem>()
```

It introduces:

- coordinated concurrent communication;
- asynchronous waiting for work;
- explicit production completion;
- natural consumer termination.

However, it has no configured capacity limit:

```text
Producer ───► [1][2][3][4][5][6][7][8]... ───► Consumer
                         ↑
                  can keep growing
```

If production continuously exceeds consumption, pending work can still accumulate.

---

## 5. Bounded Channel and Backpressure

The third implementation creates a channel with a maximum capacity:

```csharp
var channel = Channel.CreateBounded<WorkItem>(
    new BoundedChannelOptions(_settings.ChannelCapacity)
    {
        FullMode = BoundedChannelFullMode.Wait,
        SingleWriter = true,
        SingleReader = true
    });
```

For example:

```text
ChannelCapacity = 5

[1][2][3][4][5]
         FULL
```

`SingleWriter` and `SingleReader` describe this simulation: one producer and one consumer.

The key setting is:

```csharp
FullMode = BoundedChannelFullMode.Wait
```

When the channel is full, another `WriteAsync()` cannot complete until capacity becomes available:

```text
Producer attempts new item
          ↓
Channel is full
          ↓
WriteAsync waits asynchronously
          ↓
Consumer removes an item
          ↓
capacity becomes available
          ↓
WriteAsync completes
          ↓
Producer continues
```

This propagation of downstream saturation toward the producer is **backpressure**.

The producer is slowed when the consumer cannot keep up, preventing pending work from growing without limit.

---

## 6. Observing Backpressure

The bounded implementation measures the time spent waiting in `WriteAsync()`:

```csharp
var stopwatch = Stopwatch.StartNew();

await writer.WriteAsync(item, cancellationToken);

stopwatch.Stop();

if (stopwatch.ElapsedMilliseconds > 50)
{
    _logger.LogInformation(
        "BACKPRESSURE: WorkItem {WorkItemId} waited {ElapsedMilliseconds} ms for channel capacity",
        item.Id,
        stopwatch.ElapsedMilliseconds);
}
```

During the experiment, the producer generated an item approximately every `100 ms`, while the consumer required approximately `1000 ms` to process one.

Once the channel became full, the execution showed:

```text
BACKPRESSURE: WorkItem 15 waited 902 ms for channel capacity
Produced WorkItem 15
Attempting to produce WorkItem 16

Completed WorkItem 10
Consuming WorkItem 11

BACKPRESSURE: WorkItem 16 waited 900 ms for channel capacity
Produced WorkItem 16
```

Subsequent waits were approximately `895–900 ms`.

Once the buffer is saturated, the producer can only continue when the consumer frees capacity.

The wait is asynchronous: the producer method is suspended at `await writer.WriteAsync(...)`; a thread does not need to remain blocked for the entire waiting period.

---

## 7. Simulation and Application Lifecycle

`Simulation` remains the orchestrator.

For the bounded scenario it creates the channel and provides its endpoints to producer and consumer:

```text
Simulation
    │
    ├── ChannelWriter<WorkItem> ──► Producer
    │
    └── ChannelReader<WorkItem> ──► Consumer
```

Both operations execute concurrently and `Simulation` waits for both:

```csharp
await Task.WhenAll(producerTask, consumerTask);
```

When production finishes:

```text
Producer finishes
      ↓
Writer.TryComplete()
      ↓
Consumer drains pending items
      ↓
ReadAllAsync() finishes
      ↓
Task.WhenAll() finishes
      ↓
Simulation finishes
      ↓
Host shutdown
```

Unlike the naive implementation, the channel-based scenarios therefore have a natural completion mechanism.

---

## 8. Comparison

| Implementation | Concurrent coordination | Async waiting | Completion signal | Capacity limit | Backpressure |
|---|:---:|:---:|:---:|:---:|:---:|
| Naive `Queue<T>` | No | No | No | No | No |
| Unbounded `Channel<T>` | Yes | Yes | Yes | No | No |
| Bounded `Channel<T>` | Yes | Yes | Yes | Yes | Yes |

The progression is:

```text
Queue<T>
   ↓
reveals coordination problems
   ↓
Channel<T> Unbounded
   ↓
solves communication, waiting and completion
   ↓
Channel<T> Bounded
   ↓
adds capacity control and backpressure
```

---

## 9. Conclusion

The main lesson is not simply to replace `Queue<T>` with `Channel<T>`.

The exercise demonstrates how to recognize four concerns in a producer-consumer problem:

1. coordinating concurrent producers and consumers;
2. waiting efficiently when no work is available;
3. signaling when production has finished;
4. controlling accumulation when production exceeds consumption.

An unbounded `Channel<T>` solves coordination, asynchronous waiting, and completion. A bounded channel additionally limits pending work and, with `BoundedChannelFullMode.Wait`, applies backpressure when the consumer cannot keep up.
