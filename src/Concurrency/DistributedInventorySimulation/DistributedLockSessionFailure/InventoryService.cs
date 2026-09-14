using DistributedInventorySimulation.Configuration;

namespace DistributedInventorySimulation.DistributedLockSessionFailure;

public sealed class InventoryService
{
    private readonly InventoryRepository _repository;
    private readonly DistributedInventorySettings _settings;

    public InventoryService(InventoryRepository repository, DistributedInventorySettings settings)
    {
        _repository = repository;
        _settings = settings;
    }

    public Task<bool> PurchaseAsync(string instanceName, int productId, int quantity, CancellationToken cancellationToken)
    {
        return _repository.TryPurchaseAsync(instanceName, productId, quantity, _settings.OperationDelayMilliseconds, cancellationToken);
    }    
}