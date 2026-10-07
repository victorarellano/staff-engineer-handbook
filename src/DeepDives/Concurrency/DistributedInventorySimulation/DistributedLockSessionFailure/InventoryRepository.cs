using DistributedInventorySimulation.Domain;
using Npgsql;

namespace DistributedInventorySimulation.DistributedLockSessionFailure;

public sealed class InventoryRepository
{
    private readonly string _connectionString;

    public InventoryRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<bool> TryPurchaseAsync(
            string instanceName, int productId, int quantity, int operationDelayMilliseconds, CancellationToken cancellationToken)
        {
            await using var connection = new NpgsqlConnection(_connectionString);

            await connection.OpenAsync(cancellationToken);

            try
            {
                await AcquireLockAsync(connection, productId, cancellationToken);

                Console.WriteLine($"{instanceName} acquired distributed lock.");

                var stock = await ReadStockAsync(connection, productId, cancellationToken);

                Console.WriteLine($"{instanceName} reads stock {stock}.");

                if (stock < quantity)
                {
                    Console.WriteLine($"{instanceName} rejected. Not enough stock.");

                    return false;
                }

                await Task.Delay(operationDelayMilliseconds, cancellationToken);

                await UpdateStockAsync(connection, productId, stock - quantity, cancellationToken);

                Console.WriteLine($"{instanceName} completed purchase.");

                return true;
            }
            finally
            {
                await ReleaseLockAsync(connection, productId, cancellationToken);

                Console.WriteLine($"{instanceName} released distributed lock.");
            }
        }

    private static async Task ReleaseLockAsync(NpgsqlConnection connection, int lockKey, CancellationToken cancellationToken)
    {
        const string sql = "SELECT pg_advisory_unlock(@lockKey);";

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("lockKey", lockKey);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<int> ReadStockAsync(NpgsqlConnection connection, int productId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT available_stock
            FROM product_inventory
            WHERE product_id = @productId;
            """;

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("productId", productId);

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return Convert.ToInt32(result);
    }

    private static async Task UpdateStockAsync(NpgsqlConnection connection, int productId, int newStock, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE product_inventory
            SET available_stock = @newStock
            WHERE product_id = @productId;
            """;

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("productId", productId);
        command.Parameters.AddWithValue("newStock", newStock);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }       

    public async Task SimulateSessionFailureAsync(int productId, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        await AcquireLockAsync(connection, productId, cancellationToken);

        Console.WriteLine("Instance A acquired distributed lock.");

        Console.WriteLine("Instance A simulating abrupt session termination.");

        // Deliberately no pg_advisory_unlock().
        // Leaving this method disposes the connection.
    }

    private static async Task AcquireLockAsync(NpgsqlConnection connection, int lockKey, CancellationToken cancellationToken)
    {
        const string sql = "SELECT pg_advisory_lock(@lockKey);";

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("lockKey", lockKey);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }    
}
