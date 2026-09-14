using DistributedInventorySimulation.Configuration;
using DistributedInventorySimulation.Domain;
using Microsoft.Extensions.Options;

namespace DistributedInventorySimulation.Naive;

public sealed class InventoryService
{
    private readonly ProductInventory _inventory;
    private readonly DistributedInventorySettings _settings;

    public InventoryService(ProductInventory inventory, IOptions<DistributedInventorySettings> options)
    {
        _inventory = inventory;
        _settings = options.Value;
    }

    public async Task<bool> PurchaseAsync(string instanceName, int quantity, CancellationToken cancellationToken)
    {
        Console.WriteLine($"{instanceName} reads stock {_inventory.AvailableStock}");

        if (_inventory.AvailableStock < quantity)
        {
            return false;
        }

        await Task.Delay(_settings.OperationDelayMilliseconds, cancellationToken);

        _inventory.DecreaseStock(quantity);

        Console.WriteLine($"{instanceName} completed purchase. Stock {_inventory.AvailableStock}");

        return true;
    }
}