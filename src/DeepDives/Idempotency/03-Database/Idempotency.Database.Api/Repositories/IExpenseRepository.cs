using Idempotency.Api.Models;

namespace Idempotency.Api.Repositories;

public interface IExpenseRepository
{
    Task InsertAsync(Expense expense);
}