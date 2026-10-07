using ButcherShopSimulation.Configuration;
using ButcherShopSimulation.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ButcherShopSimulation.Services
{
    public class Simulation
    {
        private readonly CustomerServiceCoordinator _customerServiceCoordinator;
        private readonly Statistics _statistics;
        private readonly ButcherShopSettings _settings;
        private readonly ILogger<Simulation> _logger;

        private readonly TimeSpan _openingTime;
        private readonly TimeSpan _closingTime;
        private readonly TimeSpan _lunchStart;
        private readonly TimeSpan _lunchEnd;

        public Simulation(IOptions<ButcherShopSettings> options, ILogger<Simulation> logger)
        {
            _settings = options.Value;
            _logger = logger;

            _openingTime = TimeSpan.Parse(_settings.OpeningHour);
            _closingTime = TimeSpan.Parse(_settings.ClosingHour);
            _lunchStart = TimeSpan.Parse(_settings.LunchStart);
            _lunchEnd = TimeSpan.Parse(_settings.LunchEnd);

            _customerServiceCoordinator = new CustomerServiceCoordinator(_settings.ButchersCount, logger);
            _statistics = new Statistics();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var tasks = new List<Task>();

            for (int ticket = 1; ticket <= _settings.MaxTicketsPerDay; ticket++)
            {
                var simulatedTime = GetSimulatedTime(ticket);

                if (IsLunchTime(simulatedTime))
                {
                    _logger.LogInformation("Customer {Ticket} arrived during lunch break ({Time}), rejected.",
                        ticket, simulatedTime.ToString("T"));
                    _statistics.RegisterRejected();
                    continue;
                }

                if (IsOutsideWorkingHours(simulatedTime))
                {
                    _logger.LogInformation("Customer {Ticket} arrived outside working hours ({Time}), rejected.",
                        ticket, simulatedTime.ToString("T"));
                    _statistics.RegisterRejected();
                    continue;
                }

                var customer = new Customer(ticket);

                Task serviceTask = _customerServiceCoordinator.ServeCustomerAsync(
                    customer,
                    _statistics,
                    cancellationToken,
                    _settings.ServiceTimeMinMinutes,
                    _settings.ServiceTimeMaxMinutes);

                tasks.Add(serviceTask);


                int arrivalDelay = Random.Shared.Next(
                    _settings.ArrivalTimeMinMs,
                    _settings.ArrivalTimeMaxMs);

                await Task.Delay(arrivalDelay, cancellationToken);
            }

            await Task.WhenAll(tasks);
        }

        private bool IsOutsideWorkingHours(DateTime simulatedTime)
        {
            return simulatedTime.TimeOfDay < _openingTime || simulatedTime.TimeOfDay >= _closingTime;
        }

        private bool IsLunchTime(DateTime simulatedTime)
        {
            return simulatedTime.TimeOfDay >= _lunchStart && simulatedTime.TimeOfDay < _lunchEnd;
        }

        private DateTime GetSimulatedTime(int ticket)
        {
            return DateTime.Today.Add(_openingTime).AddMinutes(ticket * 2);
        }

        public void PrintStatistics()
        {
            _statistics.PrintReport(_logger);
        }
    }
}
