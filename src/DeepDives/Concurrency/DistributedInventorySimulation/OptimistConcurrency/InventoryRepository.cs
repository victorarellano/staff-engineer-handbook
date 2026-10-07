using DistributedInventorySimulation.Domain;
using Npgsql;

namespace DistributedInventorySimulation.OptimistConcurrence;

public sealed class InventoryRepository
{
    private readonly string _connectionString;

    public InventoryRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<bool> TryUpdateAsync(InventorySnapshot snapshot, int newStock, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        const string sql = """
            UPDATE product_inventory
            SET available_stock = @newStock,
                version = version + 1
            WHERE product_id = @productId
            AND version = @expectedVersion;
            """;

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("productId", snapshot.ProductId);
        command.Parameters.AddWithValue("newStock", newStock);
        command.Parameters.AddWithValue("expectedVersion", snapshot.Version);

        var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);

        return affectedRows == 1;
    }

    public async Task<InventorySnapshot?> GetAsync(int productId, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT product_id,
                   available_stock,
                   version
            FROM product_inventory
            WHERE product_id = @productId;
            """;

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("productId", productId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new InventorySnapshot(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2));
    }
}
