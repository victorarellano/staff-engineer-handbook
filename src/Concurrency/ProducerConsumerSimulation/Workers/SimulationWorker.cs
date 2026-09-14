using ProducerConsumerSimulation.Configuration;
using ProducerConsumerSimulation.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ProducerConsumerSimulation.Workers
{
    public class SimulationWorker : BackgroundService
    {
        private readonly ILogger<SimulationWorker> _logger;
        private readonly Simulation _simulation;
        private readonly IHostApplicationLifetime _applicationLifetime;

        public SimulationWorker(ILogger<SimulationWorker> logger, 
            Simulation simulation,
            IHostApplicationLifetime applicationLifetime)
        {
            _logger = logger;
            _simulation = simulation;
            _applicationLifetime = applicationLifetime;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("Producer-Consumer simulation started.");

                await _simulation.StartAsync(stoppingToken);

                _logger.LogInformation("Producer-Consumer simulation finished.");
            }
            finally
            {
                _applicationLifetime.StopApplication();
            }                
        }
    }
}
