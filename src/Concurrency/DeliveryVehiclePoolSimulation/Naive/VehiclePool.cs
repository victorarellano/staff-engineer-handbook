using DeliveryVehiclePoolSimulation.Domain;

namespace DeliveryVehiclePoolSimulation.Naive;

public sealed class VehiclePool
{
    private readonly List<Vehicle> _availableVehicles;
    private readonly SemaphoreSlim _availability;
    private readonly object _sync = new();

    public VehiclePool(IEnumerable<Vehicle> vehicles)
    {
        _availableVehicles = vehicles.ToList();

        _availability = new SemaphoreSlim(_availableVehicles.Count, _availableVehicles.Count);
    }

    public async Task<Vehicle?> AcquireAsync(int timeoutMilliseconds, CancellationToken cancellationToken)
    {
        var acquired = await _availability.WaitAsync(timeoutMilliseconds, cancellationToken);

        if (!acquired)
            return null;

        try
        {            
            lock (_sync)
            {
                if (_availableVehicles.Count == 0)
                    return null;

                var vehicle = _availableVehicles[0];
                _availableVehicles.RemoveAt(0);

                return vehicle;
            }
        }
        catch
        {
            _availability.Release();
            throw;
        }        
    }

    public void Release(Vehicle vehicle)
    {
        lock (_sync)
        {
            _availableVehicles.Add(vehicle);
        }

        _availability.Release();
    }
}