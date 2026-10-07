using Npgsql;

namespace Idempotency.Api.Repositories;

public sealed class PostgresUnitOfWorkFactory(NpgsqlDataSource dataSource) : IUnitOfWorkFactory
{
    public async Task<IUnitOfWork> CreateAsync()
    {
        var connection = await dataSource.OpenConnectionAsync();
        var transaction = await connection.BeginTransactionAsync();

        return new PostgresUnitOfWork(connection, transaction);
    }
}