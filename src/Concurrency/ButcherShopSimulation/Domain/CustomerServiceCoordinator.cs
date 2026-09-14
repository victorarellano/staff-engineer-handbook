using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ButcherShopSimulation.Domain
{
    public class CustomerServiceCoordinator
    {
        private ILogger _logger;
        private readonly SemaphoreSlim _serviceCapacity;
        private readonly List<Butcher> _butchers;


        public CustomerServiceCoordinator(int butchersCount, ILogger logger)
        {
            _logger = logger;
            _butchers = Enumerable.Range(1, butchersCount)
                                  .Select(id => new Butcher(id, logger))
                                  .ToList();
            _serviceCapacity = new SemaphoreSlim(butchersCount, butchersCount);
        }

        public async Task ServeCustomerAsync(Customer customer, Statistics statistics, CancellationToken cancellationToken, int serviceMin, int serviceMax)
        {
            _logger.LogInformation("Customer {Ticket} took a number at {Time}", customer.TicketNumber, customer.ArrivalTime.ToString("T"));

            var stopwatch = Stopwatch.StartNew();
            await _serviceCapacity.WaitAsync(cancellationToken);

            try
            {
                if (stopwatch.ElapsedMilliseconds > 10)
                {
                    _logger.LogInformation("Customer {Ticket} waited {Seconds:F2} seconds before being served",
                        customer.TicketNumber,
                        stopwatch.Elapsed.TotalSeconds);
                    statistics.RegisterWaitTime(stopwatch.Elapsed);
                }

                var butcher = _butchers.First();
                await butcher.AttendAsync(customer, statistics, serviceMin, serviceMax, cancellationToken);
            }
            finally
            {
                _serviceCapacity.Release();
            }
        }
    }
}
