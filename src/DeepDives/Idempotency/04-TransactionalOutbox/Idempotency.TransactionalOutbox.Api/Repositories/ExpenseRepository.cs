using Idempotency.Api.Models;
using Npgsql;

namespace Idempotency.Api.Repositories;

public sealed class ExpenseRepository(NpgsqlConnection connection, NpgsqlTransaction transaction) : IExpenseRepository
{
    public async Task InsertAsync(Expense expense)
    {
        await using var command = connection.CreateCommand();

        command.Transaction = transaction;

        command.CommandText = "INSERT INTO expenses (id, amount, description) VALUES (@id, @amount, @description);";

        command.Parameters.AddWithValue("id", expense.Id);
        command.Parameters.AddWithValue("amount", expense.Amount);
        command.Parameters.AddWithValue("description", expense.Description);

        await command.ExecuteNonQueryAsync();
    }
}