namespace DistributedInventorySimulation.SharedResourceSynchronization;

public sealed class InventoryRepository
{
    private int _availableStock;
    private readonly object _syncRoot = new();

    public InventoryRepository(int initialStock)
    {
        _availableStock = initialStock;
    }

    public bool TryPurchase(int quantity)
    {
        lock (_syncRoot)
        {
            if (_availableStock < quantity)
            {
                return false;
            }

            _availableStock -= quantity;

            return true;
        }
    }

    public int ReadStock()
    {
        lock (_syncRoot)
        {
            return _availableStock;
        }
    }
}