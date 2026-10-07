using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using ProducerConsumerSimulation.Domain;

namespace ProducerConsumerSimulation.ChannelUnbounded;

public sealed class Consumer
{
    private readonly ChannelReader<WorkItem> _reader;
    private readonly ILogger<Consumer> _logger;

    public Consumer(Channel<WorkItem> channel, ILogger<Consumer> logger)
    {
        _reader = channel.Reader;
        _logger = logger;
    }

    public async Task ConsumeAsync(int processingDelayMilliseconds, CancellationToken cancellationToken)
    {
        await foreach (var item in _reader.ReadAllAsync(cancellationToken))
        {
            _logger.LogInformation("Consuming WorkItem {WorkItemId}", item.Id);

            await Task.Delay(processingDelayMilliseconds, cancellationToken);

            _logger.LogInformation("Completed WorkItem {WorkItemId}", item.Id);
        }
    }
}