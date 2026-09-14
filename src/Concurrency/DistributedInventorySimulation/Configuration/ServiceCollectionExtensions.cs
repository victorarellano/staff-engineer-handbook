using DistributedInventorySimulation.Domain;
using DistributedInventorySimulation.Services;
using DistributedInventorySimulation.Workers;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using InventoryNaiveService = DistributedInventorySimulation.Naive.InventoryService;
using InventoryLocalLockService = DistributedInventorySimulation.LocalLock.InventoryService;

using InventoryMultipleRepository = DistributedInventorySimulation.MultipleInstance.InventoryRepository;
using InventorySharedResourceRepository = DistributedInventorySimulation.SharedResourceSynchronization.InventoryRepository;
using InventoryAtomicaDatabaseRepository = DistributedInventorySimulation.AtomicDatabaseUpdate.InventoryRepository;
using InventoryOptimistConcurrenceRepository = DistributedInventorySimulation.OptimistConcurrence.InventoryRepository;
using InventoryOptimistConcurrenceRetryRepository = DistributedInventorySimulation.OptimistConcurrence.Retry.InventoryRepository;
using DistributedLockRepository = DistributedInventorySimulation.DistributedLock.InventoryRepository;
using DistributedLockWithTimeoutRepository = DistributedInventorySimulation.DistributedLockWithTimeout.InventoryRepository;
using DistributedLockFailureRecoveryRepository = DistributedInventorySimulation.DistributedLockFailureRecovery.InventoryRepository;
using DistributedLockSessionFailureRepository = DistributedInventorySimulation.DistributedLockSessionFailure.InventoryRepository;
using Microsoft.Extensions.Options;

namespace DistributedInventorySimulation.Configuration;

/// <summary>
/// Provides dependency injection registrations for the simulation.
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
        services.Configure<DistributedInventorySettings>(configuration.GetSection(DistributedInventorySettings.SectionName));
        services.AddSingleton<Simulation>();
        services.AddSingleton<InventoryNaiveService>();
        services.AddSingleton<InventoryLocalLockService>();
        
        services.AddSingleton(new ProductInventory(productId: 1, initialStock: 1));
        services.AddSingleton<InventoryMultipleRepository>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<DistributedInventorySettings>>().Value;
            return new InventoryMultipleRepository(settings.InitialStock);
        });

        services.AddSingleton<InventorySharedResourceRepository>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<DistributedInventorySettings>>().Value;
            return new InventorySharedResourceRepository(settings.InitialStock);
        });

        services.AddSingleton<InventoryAtomicaDatabaseRepository>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            
            var connectionString = configuration.GetConnectionString("InventoryDatabase")
            ?? throw new InvalidOperationException("InventoryDatabase connection string not configured.");

            return new InventoryAtomicaDatabaseRepository(connectionString);
        });  

        services.AddSingleton<InventoryOptimistConcurrenceRepository>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            
            var connectionString = configuration.GetConnectionString("InventoryDatabase")
            ?? throw new InvalidOperationException("InventoryDatabase connection string not configured.");

            return new InventoryOptimistConcurrenceRepository(connectionString);
        }); 

        services.AddSingleton<InventoryOptimistConcurrenceRetryRepository>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            
            var connectionString = configuration.GetConnectionString("InventoryDatabase")
            ?? throw new InvalidOperationException("InventoryDatabase connection string not configured.");

            return new InventoryOptimistConcurrenceRetryRepository(connectionString);
        });

        services.AddSingleton<DistributedLockRepository>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            
            var connectionString = configuration.GetConnectionString("InventoryDatabase")
            ?? throw new InvalidOperationException("InventoryDatabase connection string not configured.");

            return new DistributedLockRepository(connectionString);
        });

        services.AddSingleton<DistributedLockWithTimeoutRepository>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            
            var connectionString = configuration.GetConnectionString("InventoryDatabase")
            ?? throw new InvalidOperationException("InventoryDatabase connection string not configured.");

            return new DistributedLockWithTimeoutRepository(connectionString);
        });

        services.AddSingleton<DistributedLockFailureRecoveryRepository>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            
            var connectionString = configuration.GetConnectionString("InventoryDatabase")
            ?? throw new InvalidOperationException("InventoryDatabase connection string not configured.");

            return new DistributedLockFailureRecoveryRepository(connectionString);
        });

        services.AddSingleton<DistributedLockSessionFailureRepository>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            
            var connectionString = configuration.GetConnectionString("InventoryDatabase")
            ?? throw new InvalidOperationException("InventoryDatabase connection string not configured.");

            return new DistributedLockSessionFailureRepository(connectionString);
        });

        services.AddHostedService<SimulationWorker>();

        return services;
    }
}
