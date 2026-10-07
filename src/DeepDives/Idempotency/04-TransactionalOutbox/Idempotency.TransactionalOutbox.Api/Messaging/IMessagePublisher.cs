using Idempotency.Api.Models;

namespace Idempotency.Api.Messaging;
public interface IMessagePublisher
{
    Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken);
}