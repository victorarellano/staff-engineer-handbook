using Idempotency.Api.Contracts;
using Idempotency.Api.Repositories;
using Idempotency.Api.Services;

namespace Idempotency.Api.Endpoints;

public static class ExpenseEndpoints
{
    public static IEndpointRouteBuilder MapExpenseEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/expenses");

        group.MapPost("/", CreateExpenseAsync);
        group.MapGet("/", GetExpensesAsync);

        return endpoints;
    }

    private static async Task<IResult> CreateExpenseAsync(
        CreateExpenseRequest request,
        HttpRequest httpRequest,
        IExpenseService expenseService,
        ILogger<Program> logger)
    {
        if (!TryGetIdempotencyKey(httpRequest, out var idempotencyKey))
        {
            return Results.BadRequest(new
            {
                error = "A valid Idempotency-Key header is required."
            });
        }

        using var scope = logger.BeginScope(
            new Dictionary<string, object>
            {
                ["Port"] = httpRequest.Host.Port ?? 0,
                ["IdempotencyKey"] = idempotencyKey
            });

        var result = await expenseService.CreateAsync(request, idempotencyKey);

        return result.AlreadyExisted ? 
            Results.Ok(result.Expense) : Results.Created($"/expenses/{result.Expense.Id}", result.Expense);
    }

    private static async Task<IResult> GetExpensesAsync(IExpenseReader expenseReader)
    {
        var expenses = await expenseReader.GetExpensesAsync();

        return Results.Ok(expenses);
    }

    private static bool TryGetIdempotencyKey(HttpRequest request, out Guid idempotencyKey)
    {
        if (!request.Headers.TryGetValue("Idempotency-Key", out var header))
        {
            idempotencyKey = default;
            return false;
        }

        return Guid.TryParse(header.ToString(), out idempotencyKey);
    }
}