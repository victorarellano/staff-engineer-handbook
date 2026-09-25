using Idempotency.Api.Contracts;
using Idempotency.Api.Models;
using Idempotency.Api.Repositories;

namespace Idempotency.Api.Services;

public sealed class ExpenseService(IUnitOfWorkFactory unitOfWorkFactory, IIdempotencyReader idempotencyReader, IExpenseLogger logger) : IExpenseService
{
    public async Task<CreateExpenseResult> CreateAsync(CreateExpenseRequest request, Guid idempotencyKey)
    {
        var expense = new Expense(Guid.NewGuid(), request.Description, request.Amount);

        logger.RequestReceived(idempotencyKey);

        await using var unitOfWork = await unitOfWorkFactory.CreateAsync();

        try
        {
            logger.CreatingExpense(expense.Id);

            await unitOfWork.Expenses.InsertAsync(expense);

            logger.TryingIdempotencyKey(idempotencyKey);

            await unitOfWork.Idempotency.InsertAsync(idempotencyKey, expense.Id);

            await unitOfWork.CommitAsync();

            logger.ExpenseCommitted(expense.Id);

            return new CreateExpenseResult(expense, AlreadyExisted: false);
        }
        catch (IdempotencyConflictException)
        {
            logger.IdempotencyRaceLost(idempotencyKey, expense.Id);

            await unitOfWork.RollbackAsync();

            var existingExpense = await idempotencyReader.FindExpenseAsync(idempotencyKey);

            if (existingExpense is null)
            {
                throw new InvalidOperationException("Idempotency state could not be recovered.");
            }

            logger.ReturningExistingExpense(existingExpense.Id);

            return new CreateExpenseResult(existingExpense, AlreadyExisted: true);
        }
    }
}