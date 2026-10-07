using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BankAccountRaceConditionSimulation.Services;
using BankAccountRaceConditionSimulation.Workers;

namespace BankAccountRaceConditionSimulation.Configuration;

/// <summary>
/// Provides dependency injection registrations for the butcher shop simulation.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the configuration and hosted services required by the simulation.
    /// </summary>
    /// <param name="services">The application's service collection.</param>
    /// <param name="configuration">The application's configuration.</param>
    /// <returns>The same service collection, allowing chained registrations.</returns>
    public static IServiceCollection AddSimulation(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<BankAccountRaceConditionSettings>(configuration.GetSection(BankAccountRaceConditionSettings.SectionName));

        services.AddSingleton<Simulation>();
        services.AddHostedService<SimulationWorker>();

        return services;
    }
}
