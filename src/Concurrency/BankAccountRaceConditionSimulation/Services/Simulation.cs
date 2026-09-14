using BankAccountRaceConditionSimulation.Configuration;
using BankAccountRaceConditionSimulation.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BankAccountRaceConditionSimulation.Services;

public class Simulation
{
    private readonly BankAccountRaceConditionSettings _settings;
    private readonly ILogger<Simulation> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public Simulation(IOptions<BankAccountRaceConditionSettings> options, ILogger<Simulation> logger, ILoggerFactory loggerFactory)
    {
        _settings = options.Value;
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await RunUnsafeScenarioAsync(cancellationToken);
        await RunSafeScenarioAsync(cancellationToken);
    }

    private async Task RunUnsafeScenarioAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Scenario 1 Unsafe withdrawal → reproduce race condition");

        var account = new BankAccount(1_000m, _loggerFactory.CreateLogger<BankAccount>());

        var withdrawalA = account.WithdrawUnsafeAsync("Withdrawal A", 700m, cancellationToken);

        var withdrawalB = account.WithdrawUnsafeAsync("Withdrawal B", 500m, cancellationToken);

        var results = await Task.WhenAll(withdrawalA, withdrawalB);

        _logger.LogInformation("Withdrawal A result: {Result}", results[0]);

        _logger.LogInformation("Withdrawal B result: {Result}", results[1]);

        _logger.LogInformation("Final balance: {Balance}", account.Balance);
    }

    private async Task RunSafeScenarioAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Scenario 2 Safe withdrawal → protects critical section");

        var account = new BankAccount(1_000m, _loggerFactory.CreateLogger<BankAccount>());

        var withdrawalA = account.WithdrawSafeAsync("Withdrawal A", 700m, cancellationToken);

        var withdrawalB = account.WithdrawSafeAsync("Withdrawal B", 500m, cancellationToken);

        var results = await Task.WhenAll(withdrawalA, withdrawalB);

        _logger.LogInformation("Withdrawal A result: {Result}", results[0]);

        _logger.LogInformation("Withdrawal B result: {Result}", results[1]);

        _logger.LogInformation("Final balance: {Balance}", account.Balance);
    }
}
