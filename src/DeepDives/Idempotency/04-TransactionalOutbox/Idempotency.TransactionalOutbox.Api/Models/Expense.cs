namespace Idempotency.Api.Models;

public sealed record Expense(Guid Id, string Description, decimal Amount);