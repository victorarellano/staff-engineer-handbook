using Microsoft.Extensions.Logging;
using ProducerConsumerSimulation.Domain;

namespace ProducerConsumerSimulation.Naive;

public sealed class Producer
{
    private readonly Queue<WorkItem> _pendingWork;
    private readonly ILogger<Producer> _logger;

    public Producer(Queue<WorkItem> pendingWork, ILogger<Producer> logger)
    {
        _pendingWork = pendingWork;
        _logger = logger;
    }

    public async Task ProduceAsync(int itemCount, int delayMilliseconds, CancellationToken cancellationToken)
    {
        for (var i = 1; i <= itemCount; i++)
        {
            var item = new WorkItem(i);

            _pendingWork.Enqueue(item);

            _logger.LogInformation("Produced WorkItem {WorkItemId}. Pending: {PendingCount}", item.Id, _pendingWork.Count);

            await Task.Delay(delayMilliseconds, cancellationToken);
        }
    }
}