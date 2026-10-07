namespace DistributedInventorySimulation.Domain;
public sealed record InventorySnapshot(int ProductId, int AvailableStock, int Version);