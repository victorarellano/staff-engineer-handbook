namespace BankTransferDeadlockSimulation.Configuration;

public sealed class BankTransferSettings
{
    public const string SectionName = "BankTransferSettings";

    public decimal InitialBalance { get; set; }

    public decimal TransferAmount { get; set; }

    public int LockAcquisitionDelayMilliseconds { get; set; }
    public int SecondLockTimeoutMilliseconds { get; set; }
    
    public int MaxRetryAttempts { get; set; }

    public int RetryDelayMilliseconds { get; set; }
}