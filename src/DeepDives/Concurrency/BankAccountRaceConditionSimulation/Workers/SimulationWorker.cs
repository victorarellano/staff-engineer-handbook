using BankAccountRaceConditionSimulation.Configuration;
using BankAccountRaceConditionSimulation.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BankAccountRaceConditionSimulation.Workers;

public class SimulationWorker : BackgroundService
{
    private readonly ILogger<SimulationWorker> _logger;
    private readonly Simulation _simulation;

    public SimulationWorker(ILogger<SimulationWorker> logger, Simulation simulation)
    {
        _logger = logger;
        _simulation = simulation;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("=== Starting Bank Account Race Condition Simulation ===");

        await _simulation.StartAsync(stoppingToken);

        _logger.LogInformation("=== End of Simulation ===");
    }
}
