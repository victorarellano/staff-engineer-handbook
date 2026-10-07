using Microsoft.Extensions.Logging;

namespace ButcherShopSimulation.Domain
{
    /// <summary>
    /// Represents a butcher capable of serving customers during the simulation.
    /// </summary>
    public class Butcher
    {
        private readonly int _id;
        private readonly ILogger _logger;

        public Butcher(int id, ILogger logger)
        {
            _id = id;
            _logger = logger;
        }

        public async Task AttendAsync(Customer customer, Statistics statistics, int minMinutes, int maxMinutes, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Butcher {ButcherId} calls customer {Ticket}", _id, customer.TicketNumber);

            var serviceTime = TimeSpan.FromMinutes(new Random().Next(minMinutes, maxMinutes + 1));
            await Task.Delay(serviceTime, cancellationToken);

            var totalTime = DateTime.Now - customer.ArrivalTime;
            statistics.RegisterService(serviceTime, totalTime);

            _logger.LogInformation(
                "Butcher {ButcherId} finished with customer {Ticket}. Total time in system: {Minutes:F2} minutes",
                _id,
                customer.TicketNumber,
                totalTime.TotalMinutes
            );
        }
        }
    }
