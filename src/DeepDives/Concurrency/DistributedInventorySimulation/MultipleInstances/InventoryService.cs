using DistributedInventorySimulation.Configuration;

namespace DistributedInventorySimulation.MultipleInstance;

public sealed class InventoryService
{
    private readonly InventoryRepository _repository;
    private readonly DistributedInventorySettings _settings;
    private readonly object _localLock = new();

    public InventoryService(InventoryRepository repository, DistributedInventorySettings settings)
    {
        _repository = repository;
        _settings = settings;
    }

    public async Task<bool> PurchaseAsync(string instanceName, int quantity, CancellationToken cancellationToken)
    {
        lock (_localLock)
        {
            var stock = _repository.ReadStock();

            Console.WriteLine($"{instanceName} reads stock {stock}, wants {quantity}");

            if (stock < quantity)
            {
                Console.WriteLine($"{instanceName} rejected. Not enough stock.");

                return false;
            }

            Thread.Sleep(_settings.OperationDelayMilliseconds);

            _repository.UpdateStock(stock - quantity);

            Console.WriteLine($"{instanceName} completed purchase. New stock {stock - quantity}");

            return true;
        }
    }
}