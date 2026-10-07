using Idempotency.Api.Models;

namespace Idempotency.Api.Repositories;

public interface IExpenseReader {
    Task<List<Expense>> GetExpensesAsync();
}
