using BankTransferDeadlockSimulation.Configuration;
using BankTransferDeadlockSimulation.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using TransferNaiveService = BankTransferDeadlockSimulation.Naive.TransferService;
using TransferOrderedService = BankTransferDeadlockSimulation.OrderedLocking.TransferService;
using TransferTimeoutService = BankTransferDeadlockSimulation.TimeoutLocking.TransferService;
using TransferBackoffService = BankTransferDeadlockSimulation.RetryWithBackoff.TransferService;

namespace BankTransferDeadlockSimulation.Services;

public sealed class Simulation
{
    private readonly BankTransferSettings _settings;
    private readonly TransferNaiveService _transferNaiveService;
    private readonly TransferOrderedService _transferOrderedService;
    private readonly TransferTimeoutService _transferTimeoutService;
    private readonly TransferBackoffService _transferBackoffService;
    
    private readonly ILogger<Simulation> _logger;

    public Simulation(
        IOptions<BankTransferSettings> options,
        TransferNaiveService transferNaiveService,
        TransferOrderedService transferOrderedService,
        TransferTimeoutService transferTimeoutService,
        TransferBackoffService transferBackoffService,
        ILogger<Simulation> logger)
    {
        _settings = options.Value;
        _transferNaiveService = transferNaiveService;
        _transferOrderedService = transferOrderedService;
        _transferTimeoutService = transferTimeoutService;
        _transferBackoffService = transferBackoffService;
        _logger = logger;
    }


    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await RunBackoffAsync(cancellationToken);
    }


    public async Task RunNaiveAsync(CancellationToken cancellationToken)
    {
        var accountA = new BankAccount(1, "Account A", _settings.InitialBalance);

        var accountB = new BankAccount(2, "Account B", _settings.InitialBalance);

        var transferAToB = Task.Run(
            () => _transferNaiveService.Transfer(accountA, accountB, _settings.TransferAmount), cancellationToken);

        var transferBToA = Task.Run(
            () => _transferNaiveService.Transfer(accountB, accountA, _settings.TransferAmount), cancellationToken);

        await Task.WhenAll(transferAToB, transferBToA);

        _logger.LogInformation(
            "Simulation completed. A={AccountA}, B={AccountB}",
            accountA.Balance,
            accountB.Balance);
    }

    public async Task RunOrderedLockingAsync(CancellationToken cancellationToken)
    {
        var accountA = new BankAccount(1, "Account A", _settings.InitialBalance);

        var accountB = new BankAccount(2, "Account B", _settings.InitialBalance);

        var transferAToB = Task.Run(
            () => _transferOrderedService.Transfer(accountA, accountB, _settings.TransferAmount), cancellationToken);

        var transferBToA = Task.Run(
            () => _transferOrderedService.Transfer(accountB, accountA, _settings.TransferAmount), cancellationToken);

        await Task.WhenAll(transferAToB, transferBToA);

        _logger.LogInformation(
            "Simulation completed. A={AccountA}, B={AccountB}",
            accountA.Balance,
            accountB.Balance);
    }    


    public async Task RunTimeoutLockingAsync(CancellationToken cancellationToken)
    {
        var accountA = new BankAccount(1, "Account A", _settings.InitialBalance);

        var accountB = new BankAccount(2, "Account B", _settings.InitialBalance);

        var transferAToB = Task.Run(
            () => _transferTimeoutService.Transfer(accountA, accountB, _settings.TransferAmount), cancellationToken);

        var transferBToA = Task.Run(
            () => _transferTimeoutService.Transfer(accountB, accountA, _settings.TransferAmount), cancellationToken);

        await Task.WhenAll(transferAToB, transferBToA);

        _logger.LogInformation(
            "Simulation completed. A={AccountA}, B={AccountB}",
            accountA.Balance,
            accountB.Balance);
    }    


    public async Task RunBackoffAsync(CancellationToken cancellationToken)
    {
        var accountA = new BankAccount(1, "Account A", _settings.InitialBalance);

        var accountB = new BankAccount(2, "Account B", _settings.InitialBalance);

        var transferAToB = Task.Run(
            () => _transferBackoffService.Transfer(accountA, accountB, _settings.TransferAmount), cancellationToken);

        var transferBToA = Task.Run(
            () => _transferBackoffService.Transfer(accountB, accountA, _settings.TransferAmount), cancellationToken);

        await Task.WhenAll(transferAToB, transferBToA);

        _logger.LogInformation(
            "Simulation completed. A={AccountA}, B={AccountB}",
            accountA.Balance,
            accountB.Balance);
    }    


}