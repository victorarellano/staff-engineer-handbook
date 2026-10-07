namespace Idempotency.Api.Repositories;

public interface IUnitOfWork : IAsyncDisposable
{
    IExpenseRepository Expenses { get; }
    IIdempotencyRepository Idempotency { get; }
    IOutboxMessageRepository OutboxMessages { get; }

    Task CommitAsync();
    Task RollbackAsync();
}