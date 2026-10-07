using Idempotency.Api.Models;
using Npgsql;

namespace Idempotency.Api.Repositories;

public sealed class IdempotencyRepository(NpgsqlConnection connection, NpgsqlTransaction transaction) : IIdempotencyRepository
{
    public async Task InsertAsync(Guid idempotencyKey, Guid expenseId)
    {
        try
        {
            await using var command = connection.CreateCommand();

            command.Transaction = transaction;
            command.CommandText = "INSERT INTO idempotency_keys (key, expense_id) VALUES (@key, @expenseId);";

            command.Parameters.AddWithValue("key", idempotencyKey);
            command.Parameters.AddWithValue("expenseId", expenseId);

            await command.ExecuteNonQueryAsync();
        }
        catch (PostgresException ex)
            when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new IdempotencyConflictException();
        }     
    }

}