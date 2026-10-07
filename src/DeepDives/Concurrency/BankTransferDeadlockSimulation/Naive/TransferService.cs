using BankTransferDeadlockSimulation.Configuration;
using BankTransferDeadlockSimulation.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BankTransferDeadlockSimulation.Naive;

public sealed class TransferService
{
    private readonly BankTransferSettings _settings;
    private readonly ILogger<TransferService> _logger;

    public TransferService(IOptions<BankTransferSettings> options, ILogger<TransferService> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public void Transfer(BankAccount source, BankAccount destination, decimal amount)
    {
        _logger.LogInformation(
            "Transfer {Source} -> {Destination} waiting for first lock",
            source.Name,
            destination.Name);

        lock (source.SyncRoot)
        {
            _logger.LogInformation(
                "Transfer {Source} -> {Destination} acquired {Source} lock",
                source.Name,
                destination.Name,
                source.Name);

            Thread.Sleep(_settings.LockAcquisitionDelayMilliseconds);

            _logger.LogInformation(
                "Transfer {Source} -> {Destination} waiting for {Destination} lock",
                source.Name,
                destination.Name,
                destination.Name);

            lock (destination.SyncRoot)
            {
                source.Withdraw(amount);
                destination.Deposit(amount);

                _logger.LogInformation(
                    "Transfer {Source} -> {Destination} completed",
                    source.Name,
                    destination.Name);
            }
        }
    }
}