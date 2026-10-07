using Idempotency.Api.Models;

namespace Idempotency.Api.Repositories;

public interface IOutboxMessageRepository
{
    Task InsertAsync(OutboxMessage outboxMessage);
    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OutboxMessage>> ClaimPendingAsync(DateTimeOffset claimedUntil, int batchSize, CancellationToken cancellationToken = default);
    Task MarkAsPublishedAsync(Guid messageId, DateTimeOffset publishedAt, CancellationToken cancellationToken = default);    
}