using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using ProducerConsumerSimulation.Domain;
using ProducerConsumerSimulation.Logging;

namespace ProducerConsumerSimulation.ChannelBounded;

public sealed class Consumer
{
    private readonly ILogger<Consumer> _logger;

    public Consumer(ILogger<Consumer> logger)
    {
        _logger = logger;
    }

    public async Task ConsumeAsync(ChannelReader<WorkItem> reader, int processingDelayMilliseconds, CancellationToken cancellationToken)
    {
        await foreach (var item in reader.ReadAllAsync(cancellationToken))
        {
            _logger.LogInformation("{Color}Consuming WorkItem {WorkItemId}{Reset}",
                ConsoleColors.Consumer,
                item.Id,
                ConsoleColors.Reset);

            await Task.Delay(processingDelayMilliseconds, cancellationToken);

            _logger.LogInformation("{Color}Completed WorkItem {WorkItemId}{Reset}",
                ConsoleColors.Consumer,
                item.Id,
                ConsoleColors.Reset);
        }
    }
}