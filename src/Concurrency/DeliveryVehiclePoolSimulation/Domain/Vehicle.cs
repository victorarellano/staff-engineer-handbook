namespace DeliveryVehiclePoolSimulation.Domain;

public sealed class Vehicle
{
    public int Id { get; }
    public string Name { get; }

    public Vehicle(int id, string name)
    {
        Id = id;
        Name = name;
    }
}