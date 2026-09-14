namespace ProducerConsumerSimulation.Configuration;

public sealed class ProducerConsumerSettings
{
    public const string SectionName = "ProducerConsumerSettings";

    public int ItemCount { get; set; }

    public int ProducerDelayMilliseconds { get; set; }

    public int ConsumerDelayMilliseconds { get; set; }

    public int EmptyQueuePollingDelayMilliseconds { get; set; }
    public int ChannelCapacity { get; set; }
}