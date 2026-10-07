using DistributedInventorySimulation.Configuration;

namespace DistributedInventorySimulation.OptimistConcurrence.Retry;

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
        for (var attempt = 1;attempt <= _settings.MaxRetryAttempts;attempt++)
        {
            var snapshot = await _repository.GetAsync(productId, cancellationToken);

            if (snapshot is null)
                return false;

            Console.WriteLine($"{instanceName} attempt {attempt}: " + $"stock {snapshot.AvailableStock}, " + $"version {snapshot.Version}");

            if (snapshot.AvailableStock < quantity)
            {
                Console.WriteLine($"{instanceName} rejected. Not enough stock.");

                return false;
            }

            await Task.Delay(_settings.OperationDelayMilliseconds, cancellationToken);

            var updated = await _repository.TryUpdateAsync(snapshot, snapshot.AvailableStock - quantity, cancellationToken);

            if (updated)
            {
                Console.WriteLine($"{instanceName} completed purchase " + $"on attempt {attempt}.");

                return true;
            }

            Console.WriteLine($"{instanceName} concurrency conflict " + $"on attempt {attempt}.");

            if (attempt < _settings.MaxRetryAttempts)
            {
                await Task.Delay(_settings.RetryDelayMilliseconds, cancellationToken);
            }
        }

        Console.WriteLine($"{instanceName} failed after maximum retry attempts.");

        return false;
    }


}