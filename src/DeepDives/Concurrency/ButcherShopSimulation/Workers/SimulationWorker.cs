using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ButcherShopSimulation.Configuration;
using ButcherShopSimulation.Services;

namespace ButcherShopSimulation.Workers
{
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
            _logger.LogInformation("=== Starting Butcher Shop Simulation ===");

            await _simulation.StartAsync(stoppingToken);

            _simulation.PrintStatistics();

            _logger.LogInformation("=== End of Simulation ===");
        }
    }
}
