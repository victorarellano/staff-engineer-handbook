using Npgsql;

namespace DistributedInventorySimulation.AtomicDatabaseUpdate;

public sealed class InventoryRepository
{
    private readonly string _connectionString;

    public InventoryRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<bool> TryPurchaseAsync(int productId, int quantity, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        const string sql = """
            UPDATE product_inventory
            SET available_stock = available_stock - @quantity
            WHERE product_id = @productId
              AND available_stock >= @quantity;
            """;

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("productId", productId);

        command.Parameters.AddWithValue("quantity", quantity);

        var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);

        return affectedRows == 1;
    }
}