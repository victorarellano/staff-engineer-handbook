namespace DistributedInventorySimulation.Domain;
public sealed class ProductInventory
{
    public int ProductId { get; }
    public int AvailableStock { get; private set; }

    public ProductInventory(int productId, int initialStock)
    {
        ProductId = productId;
        AvailableStock = initialStock;
    }

    public void DecreaseStock(int quantity)
    {
        AvailableStock -= quantity;
    }
}