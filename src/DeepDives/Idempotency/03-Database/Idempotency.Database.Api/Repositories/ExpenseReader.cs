

using Idempotency.Api.Models;
using Npgsql;

namespace Idempotency.Api.Repositories;

public sealed class ExpenseReader(NpgsqlDataSource dataSource) : IExpenseReader {
    public async Task<List<Expense>> GetExpensesAsync()
    {
        await using var connection = await dataSource.OpenConnectionAsync();

        await using var command = connection.CreateCommand();

        command.CommandText = "SELECT id, description, amount FROM expenses;";

        await using var reader = await command.ExecuteReaderAsync();

        var expenses = new List<Expense>();

        while (await reader.ReadAsync())
        {
            expenses.Add(ExpenseReader.MapExpense(reader));
        }

        return expenses;
    }

    static Expense MapExpense(NpgsqlDataReader reader)
    {
        return new Expense(reader.GetGuid(0), reader.GetString(1), reader.GetDecimal(2));
    }
}
