namespace DistributedInventorySimulation.Configuration;

public sealed class DistributedInventorySettings
{
    public const string SectionName = "DistributedInventorySettings"; 
    public int InitialStock { get; set; }
    public int OperationDelayMilliseconds { get; set; }
    public int PurchaseQuantity { get; set; }
    public int MaxRetryAttempts { get; set; }
    public int RetryDelayMilliseconds { get; set; }
    public int LockTimeoutMilliseconds { get; set; }
    public int LockRetryDelayMilliseconds { get; set; }
}