namespace DistributedInventorySimulation.MultipleInstance;
public sealed class InventoryRepository
{
    public int AvailableStock { get; private set; }

    public InventoryRepository(int initialStock)
    {
        AvailableStock = initialStock;
    }

    public int ReadStock()
    {
        return AvailableStock;
    }

    public void UpdateStock(int newStock)
    {
        AvailableStock = newStock;
    }
}