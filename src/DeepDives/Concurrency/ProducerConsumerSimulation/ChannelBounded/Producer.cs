using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using ProducerConsumerSimulation.Domain;
using ProducerConsumerSimulation.Logging;

namespace ProducerConsumerSimulation.ChannelBounded;

public sealed class Producer
{
    private readonly ILogger<Producer> _logger;

    public Producer(ILogger<Producer> logger)
    {
        _logger = logger;
    }

    public async Task ProduceAsync(ChannelWriter<WorkItem> writer, int itemCount, int delayMilliseconds, CancellationToken cancellationToken)
    {
        try
        {
            for (var i = 1; i <= itemCount; i++)
            {
                var item = new WorkItem(i);

                _logger.LogInformation("Attempting to produce WorkItem {WorkItemId}", item.Id);

                var stopwatch = Stopwatch.StartNew();

                await writer.WriteAsync(item, cancellationToken);

                stopwatch.Stop();

                if (stopwatch.ElapsedMilliseconds > 50)
                {
                    _logger.LogInformation("{Color}BACKPRESSURE: WorkItem {WorkItemId} waited {ElapsedMilliseconds} ms for channel capacity{Reset}",
                        ConsoleColors.Backpressure,
                        item.Id,
                        stopwatch.ElapsedMilliseconds,
                        ConsoleColors.Reset);
                }

                _logger.LogInformation("{Color}Produced WorkItem {WorkItemId}{Reset}", ConsoleColors.Producer, item.Id, ConsoleColors.Reset);

                await Task.Delay(delayMilliseconds, cancellationToken);
            }
        }
        finally
        {
            writer.TryComplete();
        }
    }
}