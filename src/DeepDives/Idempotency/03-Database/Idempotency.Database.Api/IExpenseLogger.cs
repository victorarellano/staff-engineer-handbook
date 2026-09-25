public interface IExpenseLogger
{
    void RequestReceived(Guid key);

    void CreatingExpense(Guid expenseId);

    void TryingIdempotencyKey(Guid key);

    void ExpenseCommitted(Guid expenseId);

    void IdempotencyRaceLost(Guid key, Guid expenseId);

    void ReturningExistingExpense(Guid expenseId);
}