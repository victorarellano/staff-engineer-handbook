using DistributedInventorySimulation.Configuration;

namespace DistributedInventorySimulation.OptimistConcurrence;

public sealed class InventoryService
{
    private readonly InventoryRepository _repository;
    private readonly DistributedInventorySettings _settings;

    public InventoryService(InventoryRepository repository, DistributedInventorySettings settings)
    {
        _repository = repository;
        _settings = settings;
    }

    public async Task<bool> PurchaseAsync(string instanceName, int productId, int quantity, CancellationToken cancellationToken)
    {
        var snapshot = await _repository.GetAsync(productId, cancellationToken);

        if (snapshot is null)
            return false;

        Console.WriteLine($"{instanceName} reads stock {snapshot.AvailableStock}, version {snapshot.Version}");

        if (snapshot.AvailableStock < quantity)
        {
            Console.WriteLine($"{instanceName} rejected. Not enough stock.");

            return false;
        }

        await Task.Delay(_settings.OperationDelayMilliseconds, cancellationToken);

        var updated = await _repository.TryUpdateAsync(snapshot, snapshot.AvailableStock - quantity, cancellationToken);

        if (!updated)
        {
            Console.WriteLine($"{instanceName} concurrency conflict. Version changed.");

            return false;
        }

        Console.WriteLine($"{instanceName} completed purchase.");

        return true;
    }
}