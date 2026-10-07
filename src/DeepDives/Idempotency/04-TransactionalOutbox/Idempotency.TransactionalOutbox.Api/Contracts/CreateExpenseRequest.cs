namespace Idempotency.Api.Contracts;

public sealed record CreateExpenseRequest(string Description, decimal Amount);