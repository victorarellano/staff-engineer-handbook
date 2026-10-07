using Npgsql;

namespace Idempotency.Api.Repositories;

public sealed class PostgresUnitOfWork : IUnitOfWork
{
    private readonly NpgsqlConnection _connection;
    private readonly NpgsqlTransaction _transaction;
    public IExpenseRepository Expenses { get; }
    public IIdempotencyRepository Idempotency { get; }    
    public IOutboxMessageRepository OutboxMessages { get; }    

    public PostgresUnitOfWork(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;

        Expenses = new ExpenseRepository(connection, transaction);
        Idempotency = new IdempotencyRepository(connection, transaction);
        OutboxMessages = new OutboxMessageRepository(connection, transaction);
    }    

    public Task CommitAsync() => _transaction.CommitAsync();

    public Task RollbackAsync() => _transaction.RollbackAsync();

    public async ValueTask DisposeAsync()
    {
        await _transaction.DisposeAsync();
        await _connection.DisposeAsync();
    }
}