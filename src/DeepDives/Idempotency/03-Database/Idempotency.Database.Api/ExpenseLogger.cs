public sealed class ExpenseLogger(ILogger<ExpenseLogger> logger) : IExpenseLogger
{
    public void RequestReceived(Guid key) => logger.LogInformation("[] Request received. Key={Key}", key);

    public void CreatingExpense(Guid expenseId) =>
        logger.LogInformation("[] Creating Expense {ExpenseId}", expenseId);

    public void TryingIdempotencyKey(Guid key) =>
        logger.LogInformation("[] Trying Idempotency-Key {Key}", key);

    public void ExpenseCommitted(Guid expenseId) =>
        logger.LogInformation("[] COMMIT Expense={ExpenseId}", expenseId);

    public void IdempotencyRaceLost(
        Guid key, Guid expenseId) => 
        logger.LogWarning("[] Lost race for Key={Key}. " + "Rolling back Expense={ExpenseId}", key, expenseId);

    public void ReturningExistingExpense(
        Guid expenseId) =>
        logger.LogInformation("[] Returning winning Expense={ExpenseId}", expenseId);
}