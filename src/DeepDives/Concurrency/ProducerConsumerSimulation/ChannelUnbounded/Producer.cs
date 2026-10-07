using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using ProducerConsumerSimulation.Domain;

namespace ProducerConsumerSimulation.ChannelUnbounded;

public sealed class Producer
{
    private readonly ChannelWriter<WorkItem> _writer;
    private readonly ILogger<Producer> _logger;

    public Producer(Channel<WorkItem> channel, ILogger<Producer> logger)
    {
        _writer = channel.Writer;
        _logger = logger;
    }

    public async Task ProduceAsync(int itemCount, int delayMilliseconds, CancellationToken cancellationToken)
    {
        try
        {
            for (var i = 1; i <= itemCount; i++)
            {
                var item = new WorkItem(i);

                await _writer.WriteAsync(item, cancellationToken);

                _logger.LogInformation("Produced WorkItem {WorkItemId}", item.Id);

                await Task.Delay(delayMilliseconds, cancellationToken);
            }
        }
        finally
        {
            _writer.TryComplete();
        }
    }
}