
using Idempotency.Api.Models;

namespace Idempotency.Api.Repositories;

public interface IIdempotencyReader
{
    Task<Expense?> FindExpenseAsync(Guid idempotencyKey);
}