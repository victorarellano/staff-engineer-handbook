namespace DistributedInventorySimulation.SharedResourceSynchronization;

public sealed class InventoryService
{
    private readonly InventoryRepository _repository;

    public InventoryService(InventoryRepository repository)
    {
        _repository = repository;
    }

    public bool Purchase(string instanceName, int quantity)
    {
        var purchased = _repository.TryPurchase(quantity);

        if (!purchased)
        {
            Console.WriteLine($"{instanceName} rejected. Not enough stock.");

            return false;
        }

        Console.WriteLine($"{instanceName} completed purchase.");

        return true;
    }
}