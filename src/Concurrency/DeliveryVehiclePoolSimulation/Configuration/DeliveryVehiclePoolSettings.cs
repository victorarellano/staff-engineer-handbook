namespace DeliveryVehiclePoolSimulation.Configuration;

public sealed class DeliveryVehiclePoolSettings
{
    public const string SectionName = "DeliveryVehiclePoolSettings";

    public int VehicleCount { get; set; }
    public int DeliveryCount { get; set; }
    public int DeliveryDurationMilliseconds { get; set; }
    public int AcquireRaceWindowMilliseconds { get; set; }
    public int FailingDeliveryId { get; set; }
    public int AcquireTimeoutMilliseconds { get; set; }
}