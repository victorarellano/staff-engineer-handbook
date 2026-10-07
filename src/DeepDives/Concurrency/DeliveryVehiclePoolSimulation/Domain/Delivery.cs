namespace DeliveryVehiclePoolSimulation.Domain;

public sealed class Delivery
{
    public int Id { get; }

    public Delivery(int id)
    {
        Id = id;
    }
}