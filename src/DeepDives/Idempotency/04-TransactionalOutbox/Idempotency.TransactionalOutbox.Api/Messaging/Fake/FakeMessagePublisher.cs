using Idempotency.Api.Messaging;
using Idempotency.Api.Models;

namespace Idempotency.Api.BackgroundServices.Fake;
public sealed class FakeMessagePublisher(
    ILogger<FakeMessagePublisher> logger) : IMessagePublisher
{
    public Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        logger.LogInformation("Publishing message {MessageId} - {Type}", message.MessageId, message.Type);

        return Task.CompletedTask;
    }
}