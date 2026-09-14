using DeliveryVehiclePoolSimulation.Configuration;
using DeliveryVehiclePoolSimulation.Domain;
using DeliveryVehiclePoolSimulation.Naive;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProducerConsumerSimulation.Logging;

namespace DeliveryVehiclePoolSimulation.Services;

public sealed class Simulation
{
    private readonly DeliveryVehiclePoolSettings _settings;
    private readonly ILogger<Simulation> _logger;

    public Simulation(IOptions<DeliveryVehiclePoolSettings> settings, ILogger<Simulation> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await RunNaiveAsync(cancellationToken);
    }    

    private async Task RunNaiveAsync(CancellationToken cancellationToken)
    {
        var vehicles = Enumerable.Range(1, _settings.VehicleCount)
            .Select(id => new Vehicle(id, $"Van #{id}"))
            .ToList();

        var pool = new VehiclePool(vehicles);

        var deliveries = Enumerable.Range(1, _settings.DeliveryCount)
            .Select(id => new Delivery(id))
            .ToList();

        var tasks = deliveries.Select(delivery => ExecuteDeliveryAsync(delivery, pool, cancellationToken));

        await Task.WhenAll(tasks);
    }

    private async Task ExecuteDeliveryAsync(Delivery delivery, VehiclePool pool, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{Color}Delivery {DeliveryId} requesting vehicle{Reset}", ConsoleColors.Request, delivery.Id, ConsoleColors.Reset);

        var vehicle = await pool.AcquireAsync(_settings.AcquireTimeoutMilliseconds, cancellationToken);

        if (vehicle is null)
        {
            _logger.LogWarning("{Color}Delivery {DeliveryId} timed out waiting for a vehicle{Reset}", ConsoleColors.Error, delivery.Id, ConsoleColors.Reset);

            return;
        }

        try
        {
            _logger.LogInformation("{Color}Delivery {DeliveryId} acquired {VehicleName}{Reset}", ConsoleColors.Acquired, delivery.Id, vehicle.Name, ConsoleColors.Reset);

            await Task.Delay(_settings.DeliveryDurationMilliseconds, cancellationToken);

            if (delivery.Id == _settings.FailingDeliveryId)
            {
                throw new InvalidOperationException($"Delivery {delivery.Id} failed.");
            }
            
            _logger.LogInformation("{Color}Delivery {DeliveryId} completed using {VehicleName}{Reset}", ConsoleColors.Completed, delivery.Id, vehicle.Name, ConsoleColors.Reset);
        }
        finally
        {
            pool.Release(vehicle);

            _logger.LogInformation("{Color}Delivery {DeliveryId} released {VehicleName}{Reset}", ConsoleColors.Released, delivery.Id, vehicle.Name, ConsoleColors.Reset);  
        }
    }
}