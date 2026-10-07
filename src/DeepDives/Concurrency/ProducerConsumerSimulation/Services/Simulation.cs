using System.Threading.Channels;
using Microsoft.Extensions.Options;
using ProducerConsumerSimulation.Configuration;
using ProducerConsumerSimulation.Domain;
using UnboundedConsumer = ProducerConsumerSimulation.ChannelUnbounded.Consumer;
using UnboundedProducer = ProducerConsumerSimulation.ChannelUnbounded.Producer;
using BoundedConsumer = ProducerConsumerSimulation.ChannelBounded.Consumer;
using BoundedProducer = ProducerConsumerSimulation.ChannelBounded.Producer;
using NaiveConsumer = ProducerConsumerSimulation.Naive.Consumer;
using NaiveProducer = ProducerConsumerSimulation.Naive.Producer;

namespace ProducerConsumerSimulation.Services;

public sealed class Simulation
{
    private readonly NaiveProducer _naiveProducer;
    private readonly NaiveConsumer _naiveConsumer;

    private readonly UnboundedProducer _unboundedProducer;
    private readonly UnboundedConsumer _unboundedConsumer;
    private readonly BoundedProducer _boundedProducer;
    private readonly BoundedConsumer _boundedConsumer;
    private readonly ProducerConsumerSettings _settings;

    public Simulation(NaiveProducer naiveProducer, NaiveConsumer naiveConsumer, 
        UnboundedProducer unboundedProducer, UnboundedConsumer unboundedConsumer, 
        BoundedProducer boundedProducer, BoundedConsumer boundedConsumer, 
        IOptions<ProducerConsumerSettings> options)
    {
        _naiveProducer = naiveProducer;
        _naiveConsumer = naiveConsumer;

        _unboundedProducer = unboundedProducer;
        _unboundedConsumer = unboundedConsumer;

        _boundedProducer = boundedProducer;
        _boundedConsumer = boundedConsumer;        

        _settings = options.Value;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await RunBoundedChannelScenarioAsync(cancellationToken);
    }

    private async Task RunNaiveScenarioAsync(CancellationToken cancellationToken)
    {
        var producerTask = _naiveProducer.ProduceAsync(_settings.ItemCount, _settings.ProducerDelayMilliseconds, cancellationToken);

        var consumerTask = _naiveConsumer.ConsumeAsync(_settings.ConsumerDelayMilliseconds, _settings.EmptyQueuePollingDelayMilliseconds, cancellationToken);

        await Task.WhenAll(producerTask, consumerTask);
    }

    private async Task RunUnboundedChannelScenarioAsync(CancellationToken cancellationToken)
    {
        var producerTask = _unboundedProducer.ProduceAsync(_settings.ItemCount, _settings.ProducerDelayMilliseconds, cancellationToken);

        var consumerTask = _unboundedConsumer.ConsumeAsync(_settings.ConsumerDelayMilliseconds, cancellationToken);

        await Task.WhenAll(producerTask, consumerTask);
    }     

    private async Task RunBoundedChannelScenarioAsync(CancellationToken cancellationToken)
    {
        var channel = Channel.CreateBounded<WorkItem>(
            new BoundedChannelOptions(_settings.ChannelCapacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleWriter = true,
                SingleReader = true
        });

        var producerTask = _boundedProducer.ProduceAsync(
            channel.Writer, _settings.ItemCount, _settings.ProducerDelayMilliseconds, cancellationToken);

        var consumerTask = _boundedConsumer.ConsumeAsync(
            channel.Reader, _settings.ConsumerDelayMilliseconds, cancellationToken);

        await Task.WhenAll(producerTask, consumerTask);
    }
}