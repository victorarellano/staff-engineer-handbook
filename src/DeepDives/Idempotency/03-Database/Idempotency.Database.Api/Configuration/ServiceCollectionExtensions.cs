using Idempotency.Api.Repositories;
using Idempotency.Api.Services;
using Npgsql;

namespace Idempotency.Api.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' not found.");

        services.AddSingleton(NpgsqlDataSource.Create(connectionString));

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDatabase(configuration);
        services.AddSingleton<IExpenseLogger, ExpenseLogger>();
        services.AddScoped<IExpenseService, ExpenseService>();
        services.AddScoped<IUnitOfWorkFactory,PostgresUnitOfWorkFactory>();
        services.AddScoped<IIdempotencyReader, IdempotencyReader>();
        services.AddScoped<IExpenseReader, ExpenseReader>();

        return services;
    }    
}