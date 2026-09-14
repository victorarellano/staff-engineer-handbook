using DistributedInventorySimulation.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using InventoryNaiveService = DistributedInventorySimulation.Naive.InventoryService;
using InventoryLocalLockService = DistributedInventorySimulation.LocalLock.InventoryService;

using InventoryMultipleService = DistributedInventorySimulation.MultipleInstance.InventoryService;
using MultipleInventoryRepository = DistributedInventorySimulation.MultipleInstance.InventoryRepository;

using InventorySharedResourceService = DistributedInventorySimulation.SharedResourceSynchronization.InventoryService;
using SharedResourceInventoryRepository = DistributedInventorySimulation.SharedResourceSynchronization.InventoryRepository;

using InventoryAtomicDatabaseService = DistributedInventorySimulation.AtomicDatabaseUpdate.InventoryService;
using AtomicDatabaseInventoryRepository = DistributedInventorySimulation.AtomicDatabaseUpdate.InventoryRepository;

using InventoryOptimistConsurrenceService = DistributedInventorySimulation.OptimistConcurrence.InventoryService;
using OptimistConcurrenceInventoryRepository = DistributedInventorySimulation.OptimistConcurrence.InventoryRepository;

using InventoryOptimistConsurrenceRetryService = DistributedInventorySimulation.OptimistConcurrence.Retry.InventoryService;
using OptimistConcurrenceInventoryRetryRepository = DistributedInventorySimulation.OptimistConcurrence.Retry.InventoryRepository;

using DistributedLockService = DistributedInventorySimulation.DistributedLock.InventoryService;
using DistributedLockRepository = DistributedInventorySimulation.DistributedLock.InventoryRepository;

using DistributedLockWithTimeoutService = DistributedInventorySimulation.DistributedLockWithTimeout.InventoryService;
using DistributedLockWithTimeoutRepository = DistributedInventorySimulation.DistributedLockWithTimeout.InventoryRepository;

using DistributedLockFailureRecoveryService = DistributedInventorySimulation.DistributedLockFailureRecovery.InventoryService;
using DistributedLockFailureRecoveryRepository = DistributedInventorySimulation.DistributedLockFailureRecovery.InventoryRepository;

using DistributedLockSessionFailureService = DistributedInventorySimulation.DistributedLockSessionFailure.InventoryService;
using DistributedLockSessionFailureRepository = DistributedInventorySimulation.DistributedLockSessionFailure.InventoryRepository;


namespace DistributedInventorySimulation.Services;

public sealed class Simulation
{
    private readonly InventoryNaiveService _inventoryNaiveService;
    private readonly InventoryLocalLockService _inventoryLocalLockService;
    private readonly MultipleInventoryRepository _repositoryMultiple;
    private readonly SharedResourceInventoryRepository _repositoryShared;
    private readonly AtomicDatabaseInventoryRepository _repositoryAtomic;
    private readonly OptimistConcurrenceInventoryRepository _repositoryOptimist;
    private readonly OptimistConcurrenceInventoryRetryRepository _repositoryRetry;
    private readonly DistributedLockRepository _repositoryDistributed;
    private readonly DistributedLockWithTimeoutRepository _repositoryDistributedTimeout;
    private readonly DistributedLockFailureRecoveryRepository _repositoryFailureRecovery;
    private readonly DistributedLockSessionFailureRepository _repositoryDistributedSessionFailure;
    private readonly DistributedInventorySettings _settings;
    private readonly ILogger<Simulation> _logger;

    public Simulation(
            IOptions<DistributedInventorySettings> options, 
            ILogger<Simulation> logger,
            InventoryNaiveService inventoryNaiveService,
            InventoryLocalLockService inventoryLocalLockService,
            MultipleInventoryRepository repository,
            SharedResourceInventoryRepository repositoryShared,
            AtomicDatabaseInventoryRepository repositoryAtomic,
            OptimistConcurrenceInventoryRepository repositoryOptimist,
            OptimistConcurrenceInventoryRetryRepository repositoryRetry,
            DistributedLockRepository repositoryDistributed,
            DistributedLockWithTimeoutRepository repositoryDistributedTimeout,
            DistributedLockFailureRecoveryRepository repositoryFailureRecovery,
            DistributedLockSessionFailureRepository repositoryDistributedSessionFailure)
    {
        _settings = options.Value;
        _logger = logger;
        _inventoryNaiveService = inventoryNaiveService;
        _inventoryLocalLockService = inventoryLocalLockService;
        _repositoryMultiple = repository;
        _repositoryShared = repositoryShared;
        _repositoryAtomic = repositoryAtomic;
        _repositoryOptimist = repositoryOptimist;
        _repositoryRetry = repositoryRetry;
        _repositoryDistributed = repositoryDistributed;
        _repositoryDistributedTimeout = repositoryDistributedTimeout;
        _repositoryFailureRecovery = repositoryFailureRecovery;
        _repositoryDistributedSessionFailure = repositoryDistributedSessionFailure;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await RunDistributedLockFailureRecoveryInstanceAsync(cancellationToken);
    }

    public async Task RunNaiveAsync(CancellationToken cancellationToken)
    {
        var purchaseA = _inventoryNaiveService.PurchaseAsync("Instance A", 1, cancellationToken);

        var purchaseB = _inventoryNaiveService.PurchaseAsync("Instance B", 1, cancellationToken);

        var results = await Task.WhenAll(purchaseA, purchaseB);

        _logger.LogInformation("Simulation completed");
    }

    public async Task RunLocalLockAsync(CancellationToken cancellationToken)
    {
        var purchaseA = _inventoryLocalLockService.PurchaseAsync("Instance A", 1, cancellationToken);

        var purchaseB = _inventoryLocalLockService.PurchaseAsync("Instance B", 1, cancellationToken);

        var results = await Task.WhenAll(purchaseA, purchaseB);

        _logger.LogInformation("Simulation completed");
    }

    public async Task RunMultiplesInstanceAsync(CancellationToken cancellationToken)
    {
        var instanceA = new InventoryMultipleService(_repositoryMultiple, _settings);

        var instanceB = new InventoryMultipleService(_repositoryMultiple, _settings);

        var purchaseA = Task.Run(() => instanceA.PurchaseAsync("Instance A", _settings.PurchaseQuantity, cancellationToken));
        var purchaseB = Task.Run(() => instanceB.PurchaseAsync("Instance B", _settings.PurchaseQuantity, cancellationToken));

        var results = await Task.WhenAll(purchaseA, purchaseB);

        _logger.LogInformation("Simulation completed");
    }

    public async Task RunSharedResourceInstanceAsync(CancellationToken cancellationToken)
    {
        var instanceA = new InventorySharedResourceService(_repositoryShared);

        var instanceB = new InventorySharedResourceService(_repositoryShared);

        var purchaseA = Task.Run(() => instanceA.Purchase("Instance A", _settings.PurchaseQuantity));
        var purchaseB = Task.Run(() => instanceB.Purchase("Instance B", _settings.PurchaseQuantity));

        var results = await Task.WhenAll(purchaseA, purchaseB);

        _logger.LogInformation("Simulation completed");
    }

    public async Task RunAtomicDatabaseInstanceAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Entering Atomic Database scenario");

        var instanceA = new InventoryAtomicDatabaseService(_repositoryAtomic);
        var instanceB = new InventoryAtomicDatabaseService(_repositoryAtomic);

        _logger.LogInformation("Starting purchases");

        var purchaseA = instanceA.PurchaseAsync("Instance A", 1, _settings.PurchaseQuantity, cancellationToken);
        var purchaseB = instanceB.PurchaseAsync("Instance B", 1, _settings.PurchaseQuantity, cancellationToken);

        var results = await Task.WhenAll(purchaseA, purchaseB);

        _logger.LogInformation("Simulation completed. Successful: {Successful}, Rejected: {Rejected}", results.Count(x => x), results.Count(x => !x));
    }

    public async Task RunOptimistConcurrenceInstanceAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Entering OptimistConcurrence scenario");

        var instanceA = new InventoryOptimistConsurrenceService(_repositoryOptimist, _settings);
        var instanceB = new InventoryOptimistConsurrenceService(_repositoryOptimist, _settings);

        _logger.LogInformation("Starting purchases");

        var purchaseA = instanceA.PurchaseAsync("Instance A", 1, _settings.PurchaseQuantity, cancellationToken);
        var purchaseB = instanceB.PurchaseAsync("Instance B", 1, _settings.PurchaseQuantity, cancellationToken);

        var results = await Task.WhenAll(purchaseA, purchaseB);

        _logger.LogInformation("Simulation completed. Successful: {Successful}, Rejected: {Rejected}", results.Count(x => x), results.Count(x => !x));
    }    

    public async Task RunOptimistConcurrenceRetryInstanceAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Entering OptimistConcurrence Retry scenario");

        var instanceA = new InventoryOptimistConsurrenceRetryService(_repositoryRetry, _settings);
        var instanceB = new InventoryOptimistConsurrenceRetryService(_repositoryRetry, _settings);

        _logger.LogInformation("Starting purchases");

        await _repositoryRetry.ResetAsync(productId: 1, initialStock: _settings.InitialStock, cancellationToken);        

        var purchaseA = instanceA.PurchaseAsync("Instance A", 1, _settings.PurchaseQuantity, cancellationToken);
        var purchaseB = instanceB.PurchaseAsync("Instance B", 1, _settings.PurchaseQuantity, cancellationToken);

        var results = await Task.WhenAll(purchaseA, purchaseB);

        _logger.LogInformation("Simulation completed. Successful: {Successful}, Rejected: {Rejected}", results.Count(x => x), results.Count(x => !x));
    }    

    public async Task RunDistributedLockInstanceAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Entering DistributedLock Retry scenario");

        var instanceA = new DistributedLockService(_repositoryDistributed, _settings);
        var instanceB = new DistributedLockService(_repositoryDistributed, _settings);

        _logger.LogInformation("Starting purchases");

        var purchaseA = instanceA.PurchaseAsync("Instance A", 1, _settings.PurchaseQuantity, cancellationToken);
        var purchaseB = instanceB.PurchaseAsync("Instance B", 1, _settings.PurchaseQuantity, cancellationToken);

        var results = await Task.WhenAll(purchaseA, purchaseB);

        _logger.LogInformation("Simulation completed. Successful: {Successful}, Rejected: {Rejected}", results.Count(x => x), results.Count(x => !x));
    } 

    public async Task RunDistributedLockWithTimeoutInstanceAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Entering DistributedLock With Timeout Retry scenario");

        var instanceA = new DistributedLockWithTimeoutService(_repositoryDistributedTimeout, _settings);
        var instanceB = new DistributedLockWithTimeoutService(_repositoryDistributedTimeout, _settings);

        _logger.LogInformation("Starting purchases");

        var purchaseA = instanceA.PurchaseAsync("Instance A", 1, _settings.PurchaseQuantity, cancellationToken);
        var purchaseB = instanceB.PurchaseAsync("Instance B", 1, _settings.PurchaseQuantity, cancellationToken);

        var results = await Task.WhenAll(purchaseA, purchaseB);

        _logger.LogInformation("Simulation completed. Successful: {Successful}, Rejected: {Rejected}", results.Count(x => x), results.Count(x => !x));
    }        

    public async Task RunDistributedLockFailureRecoveryInstanceAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Entering DistributedLock Failure Recovery scenario");

        var instanceA = new DistributedLockFailureRecoveryService(_repositoryFailureRecovery, _settings);
        var instanceB = new DistributedLockFailureRecoveryService(_repositoryFailureRecovery, _settings);

        _logger.LogInformation("Starting purchases");

        var purchaseA = instanceA.PurchaseAsync("Instance A", 1, _settings.PurchaseQuantity, true, cancellationToken);
        var purchaseB = instanceB.PurchaseAsync("Instance B", 1, _settings.PurchaseQuantity, false, cancellationToken);

        var results = await Task.WhenAll(purchaseA, purchaseB);

        _logger.LogInformation("Simulation completed. Successful: {Successful}, Rejected: {Rejected}", results.Count(x => x), results.Count(x => !x));
    }        

    public async Task RunDistributedLockSessionFailureAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Entering DistributedLock Session Failure scenario");

        var instanceB = new DistributedLockSessionFailureService(_repositoryDistributedSessionFailure, _settings);

        _logger.LogInformation("Instance A acquiring lock and closing session without unlock");

        await _repositoryDistributedSessionFailure.SimulateSessionFailureAsync(productId: 1, cancellationToken);

        _logger.LogInformation("Starting Instance B after Instance A session was closed");

        var result = await instanceB.PurchaseAsync("Instance B", 1, _settings.PurchaseQuantity, cancellationToken);

        _logger.LogInformation("Simulation completed. Successful: {Successful}", result);
    }
}