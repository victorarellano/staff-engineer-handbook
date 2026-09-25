using Idempotency.Api.Models;

namespace Idempotency.Api.Repositories;

public interface IIdempotencyRepository
{
    Task InsertAsync(Guid idempotencyKey, Guid expenseId);
}