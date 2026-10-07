namespace Idempotency.Api.Events;
public sealed record ExpenseCreated(Guid ExpenseId, decimal Amount, string Description);