using System.Collections.Concurrent;
using Idempotency.Api.Contracts;
using Idempotency.Api.Models;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

var expenses = new ConcurrentDictionary<Guid, Expense>();

app.MapPost("/expenses", (CreateExpenseRequest request) =>
{
    var expense = new Expense(Guid.NewGuid(), request.Description, request.Amount);

    expenses[expense.Id] = expense;

    return Results.Created($"/expenses/{expense.Id}", expense);
});

app.MapGet("/expenses", () => Results.Ok(expenses.Values));

app.Run();

public partial class Program;