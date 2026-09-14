using DistributedInventorySimulation.Domain;
using Npgsql;

namespace DistributedInventorySimulation.DistributedLockWithTimeout;

public sealed class InventoryRepository
{
    private readonly string _connectionString;

    public InventoryRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<bool> TryPurchaseAsync(
        string instanceName, int productId, int quantity, int operationDelayMilliseconds, int lockTimeoutMilliseconds, int lockRetryDelayMilliseconds, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);

        var lockAcquired = false;

        try
        {
            await connection.OpenAsync(cancellationToken);

            lockAcquired = await TryAcquireLockAsync(connection, productId, lockTimeoutMilliseconds, lockRetryDelayMilliseconds, cancellationToken);

            if (!lockAcquired)
            {
                Console.WriteLine($"{instanceName} could not acquire distributed lock within timeout.");
                return false;
            }

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
        catch (OperationCanceledException)
        {
            Console.WriteLine($"{instanceName} operation cancelled.");

            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{instanceName} failed: {ex.Message}");

            throw;
        }
        finally
        {
            if (lockAcquired)
            {
                try
                {
                    await ReleaseLockAsync(connection, productId, CancellationToken.None);

                    Console.WriteLine($"{instanceName} released distributed lock.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{instanceName} failed to release distributed lock: {ex.Message}");
                }
            }
        }
    }

    private static async Task<bool> TryAcquireLockAsync(
        NpgsqlConnection connection, int lockKey, int timeoutMilliseconds, int retryDelayMilliseconds, CancellationToken cancellationToken)
    {
        var startedAt = DateTime.UtcNow;

        while (DateTime.UtcNow - startedAt < TimeSpan.FromMilliseconds(timeoutMilliseconds))
        {
            const string sql = "SELECT pg_try_advisory_lock(@lockKey);";

            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("lockKey", lockKey);

            var result = await command.ExecuteScalarAsync(cancellationToken);

            if (result is true)
                return true;

            await Task.Delay(retryDelayMilliseconds, cancellationToken);
        }

        return false;
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

}
