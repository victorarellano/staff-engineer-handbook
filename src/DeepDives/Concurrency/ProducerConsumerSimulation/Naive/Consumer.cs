using Microsoft.Extensions.Logging;
using ProducerConsumerSimulation.Domain;

namespace ProducerConsumerSimulation.Naive;

public sealed class Consumer
{
    private readonly Queue<WorkItem> _pendingWork;
    private readonly ILogger<Consumer> _logger;

    public Consumer(Queue<WorkItem> pendingWork, ILogger<Consumer> logger)
    {
        _pendingWork = pendingWork;
        _logger = logger;
    }
    public async Task ConsumeAsync(int delayMilliseconds, int emptyQueuePollingDelayMilliseconds, CancellationToken cancellationToken)    
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_pendingWork.Count == 0)
            {
                await Task.Delay(50, cancellationToken);

                continue;
            }

            var item = _pendingWork.Dequeue();

            _logger.LogInformation("Consuming WorkItem {WorkItemId}. Pending: {PendingCount}", item.Id, _pendingWork.Count);

            await Task.Delay(delayMilliseconds, cancellationToken);

            _logger.LogInformation("Completed WorkItem {WorkItemId}", item.Id);
        }
    }
}