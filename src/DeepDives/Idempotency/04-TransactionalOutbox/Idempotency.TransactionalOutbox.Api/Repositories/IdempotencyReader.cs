using Idempotency.Api.Models;
using Npgsql;

namespace Idempotency.Api.Repositories;

public sealed class IdempotencyReader(NpgsqlDataSource dataSource) : IIdempotencyReader
{
    public async Task<Expense?> FindExpenseAsync(Guid idempotencyKey)
    {
        await using var connection = await dataSource.OpenConnectionAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT e.id, e.description, e.amount FROM idempotency_keys i JOIN expenses e ON e.id = i.expense_id WHERE i.key = @key;";
        command.Parameters.AddWithValue("key", idempotencyKey);

        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new Expense(reader.GetGuid(0), reader.GetString(1), reader.GetDecimal(2));
    }
}