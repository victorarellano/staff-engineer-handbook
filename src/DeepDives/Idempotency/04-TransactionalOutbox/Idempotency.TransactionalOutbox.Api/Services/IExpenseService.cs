using Idempotency.Api.Contracts;
using Idempotency.Api.Models;

namespace Idempotency.Api.Services;

public interface IExpenseService
{
    Task<CreateExpenseResult> CreateAsync(CreateExpenseRequest request, Guid idempotencyKey);
}

public sealed record CreateExpenseResult(Expense Expense, bool AlreadyExisted);