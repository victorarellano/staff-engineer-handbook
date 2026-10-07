using Idempotency.Api.Configuration;
using Idempotency.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices(builder.Configuration);

var app = builder.Build();

app.MapExpenseEndpoints();

app.Run();

public partial class Program;