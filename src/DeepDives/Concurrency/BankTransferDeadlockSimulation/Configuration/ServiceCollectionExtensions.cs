using BankTransferDeadlockSimulation.Naive;
using BankTransferDeadlockSimulation.Services;
using BankTransferDeadlockSimulation.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using TransferNaiveService = BankTransferDeadlockSimulation.Naive.TransferService;
using TransferOrderedService = BankTransferDeadlockSimulation.OrderedLocking.TransferService;
using TransferTimeoutService = BankTransferDeadlockSimulation.TimeoutLocking.TransferService;
using TransferBackoffService = BankTransferDeadlockSimulation.RetryWithBackoff.TransferService;

namespace BankTransferDeadlockSimulation.Configuration;

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
        services.Configure<BankTransferSettings>(configuration.GetSection(BankTransferSettings.SectionName));

        services.AddSingleton<Simulation>();
        services.AddSingleton<TransferNaiveService>();
        services.AddSingleton<TransferOrderedService>();
        services.AddSingleton<TransferTimeoutService>();
        services.AddSingleton<TransferBackoffService>();
        services.AddHostedService<SimulationWorker>();

        return services;
    }
}
