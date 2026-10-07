namespace Idempotency.Api.Models;

public sealed record OutboxMessage(Guid MessageId, string Type, string Payload, DateTimeOffset CreatedAt, DateTimeOffset? PublishedAt);