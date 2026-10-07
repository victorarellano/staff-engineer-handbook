using System.Threading.Tasks;

namespace DistributedInventorySimulation.AtomicDatabaseUpdate;

public sealed class InventoryService
{
    private readonly InventoryRepository _repository;

    public InventoryService(InventoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> PurchaseAsync(string instanceName, int productId, int quantity, CancellationToken cancellationToken)
    {
        var purchased = await _repository.TryPurchaseAsync(productId, quantity, cancellationToken);

        if (!purchased)
        {
            Console.WriteLine($"{instanceName} rejected. Not enough stock.");

            return false;
        }

        Console.WriteLine($"{instanceName} completed purchase.");

        return true;
    }
}