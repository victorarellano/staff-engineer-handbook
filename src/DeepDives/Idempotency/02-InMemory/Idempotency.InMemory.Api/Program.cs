using System.Collections.Concurrent;
using Idempotency.Api.Contracts;
using Idempotency.Api.Models;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

var operations = new ConcurrentDictionary<Guid, Expense>();

app.MapPost("/expenses", (CreateExpenseRequest request, HttpRequest httpRequest) =>
{
    
    
    if (!httpRequest.Headers.TryGetValue("Idempotency-Key", out var idempotencyHeaderKey))
    {
        return Results.BadRequest(new { error = "Idempotency-Key header is required." });
    }

    if (!Guid.TryParse(idempotencyHeaderKey.ToString(), out var idempotencyKey))
    {
        return Results.BadRequest(new { error = "Idempotency-Key must be a valid UUID." });
    }    

    var operationLock = new ConcurrentDictionary<Guid, object>();

    var lockObject = operationLock.GetOrAdd(idempotencyKey, _ => new object());

    lock(lockObject)
    {
        if (operations.TryGetValue(idempotencyKey, out var existingExpense))
        {
            return Results.Ok(existingExpense);
        }

        var expense = new Expense(Guid.NewGuid(), request.Description, request.Amount);
        operations[idempotencyKey] = expense;
            
        return Results.Created($"/expenses/{expense.Id}", expense);
    }
});


app.MapGet("/expenses", () => Results.Ok(operations.Values));

app.Run();

public partial class Program;