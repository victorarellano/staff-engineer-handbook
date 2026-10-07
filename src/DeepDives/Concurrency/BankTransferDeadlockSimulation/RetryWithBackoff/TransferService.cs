using BankTransferDeadlockSimulation.Configuration;
using BankTransferDeadlockSimulation.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BankTransferDeadlockSimulation.RetryWithBackoff;

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
        for (var attempt = 1; attempt <= _settings.MaxRetryAttempts; attempt++)
        {
            var secondLockTaken = false;

            _logger.LogInformation(
                "Transfer {Source} -> {Destination}, attempt {Attempt}",
                sourceAccount.Name,
                destinationAccount.Name,
                attempt);

            lock (sourceAccount.SyncRoot)
            {
                Thread.Sleep(_settings.LockAcquisitionDelayMilliseconds);

                try
                {
                    Monitor.TryEnter(
                        destinationAccount.SyncRoot,
                        _settings.SecondLockTimeoutMilliseconds,
                        ref secondLockTaken);

                    if (secondLockTaken)
                    {
                        sourceAccount.Withdraw(amount);
                        destinationAccount.Deposit(amount);

                        _logger.LogInformation(
                            "Transfer {Source} -> {Destination} completed on attempt {Attempt}",
                            sourceAccount.Name,
                            destinationAccount.Name,
                            attempt);

                        return;
                    }

                    _logger.LogWarning(
                        "Transfer {Source} -> {Destination} could not acquire second lock on attempt {Attempt}",
                        sourceAccount.Name,
                        destinationAccount.Name,
                        attempt);
                }
                finally
                {
                    if (secondLockTaken)
                    {
                        Monitor.Exit(destinationAccount.SyncRoot);
                    }
                }
            }

            var baseDelay = _settings.RetryDelayMilliseconds * attempt;

            var jitter = Random.Shared.Next(0, _settings.RetryDelayMilliseconds);

            var retryDelay = baseDelay + jitter;

            _logger.LogInformation(
                "Retrying transfer {Source} -> {Destination} in {Delay} ms",
                sourceAccount.Name,
                destinationAccount.Name,
                retryDelay);

            Thread.Sleep(retryDelay);
        }

        _logger.LogError(
            "Transfer {Source} -> {Destination} failed after {MaxAttempts} attempts",
            sourceAccount.Name,
            destinationAccount.Name,
            _settings.MaxRetryAttempts);
    }    
}