using BankTransferDeadlockSimulation.Configuration;
using BankTransferDeadlockSimulation.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BankTransferDeadlockSimulation.TimeoutLocking;

public sealed class TransferService
{
    private readonly BankTransferSettings _settings;
    private readonly ILogger<TransferService> _logger;

    public TransferService(IOptions<BankTransferSettings> options, ILogger<TransferService> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }
    
    public void Transfer(BankAccount sourceAccount, BankAccount destinationAccount, decimal amount)
    {
        var accountsInLockOrder = new[]
        {
            sourceAccount,
            destinationAccount
        }
        .OrderBy(account => account.Id)
        .ToArray();

        var firstLockAccount = accountsInLockOrder[0];
        var secondLockAccount = accountsInLockOrder[1];

        _logger.LogInformation(
            "Transfer {Source} -> {Destination} waiting for {First} lock",
            sourceAccount.Name,
            destinationAccount.Name,
            firstLockAccount.Name);

        lock (firstLockAccount.SyncRoot)
        {
                _logger.LogInformation(
                    "Transfer {Source} -> {Destination} waiting for {Second} lock",
                    sourceAccount.Name,
                    destinationAccount.Name,
                    secondLockAccount.Name);

            Thread.Sleep(_settings.LockAcquisitionDelayMilliseconds);

            var lockTaken = false;
            try
            {
                Monitor.TryEnter(secondLockAccount.SyncRoot, _settings.SecondLockTimeoutMilliseconds, ref lockTaken);

                if (!lockTaken)
                {
                    _logger.LogWarning("Could not acquire second lock for {Account}", secondLockAccount.Name);

                    return;
                }

                sourceAccount.Withdraw(amount);
                destinationAccount.Deposit(amount);

                _logger.LogInformation(
                    "Transfer {Source} -> {Destination} completed",
                    sourceAccount.Name,
                    destinationAccount.Name);
            }
            finally
            {
                if (lockTaken)
                {
                    Monitor.Exit(secondLockAccount.SyncRoot);
                }
            }
        }
    }    
}