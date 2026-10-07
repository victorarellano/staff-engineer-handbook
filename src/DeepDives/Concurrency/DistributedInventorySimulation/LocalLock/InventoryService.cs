using DistributedInventorySimulation.Configuration;
using DistributedInventorySimulation.Domain;
using Microsoft.Extensions.Options;

namespace DistributedInventorySimulation.LocalLock;

public sealed class InventoryService
{
    private readonly ProductInventory _inventory;
    private readonly DistributedInventorySettings _settings;

    private readonly object _inventoryLock = new();

    public InventoryService(ProductInventory inventory, IOptions<DistributedInventorySettings> options)
    {
        _inventory = inventory;
        _settings = options.Value;
    }

    public async Task<bool> PurchaseAsync(string instanceName, int quantity, CancellationToken cancellationToken)
    {
        await Task.Delay(_settings.OperationDelayMilliseconds, cancellationToken);

        lock (_inventoryLock)
        {
            Console.WriteLine($"{instanceName} reads stock {_inventory.AvailableStock}");

            if (_inventory.AvailableStock < quantity)
            {
                Console.WriteLine($"{instanceName} rejected. Not enough stock.");

                return false;
            }

            _inventory.DecreaseStock(quantity);

            Console.WriteLine($"{instanceName} completed purchase. Stock {_inventory.AvailableStock}");

            return true;
        }
    }
}