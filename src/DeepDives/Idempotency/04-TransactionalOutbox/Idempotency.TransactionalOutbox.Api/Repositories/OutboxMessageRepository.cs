using Idempotency.Api.Models;
using Npgsql;

namespace Idempotency.Api.Repositories;

public sealed class OutboxMessageRepository(NpgsqlConnection connection, NpgsqlTransaction transaction) : IOutboxMessageRepository
{
    public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT message_id, type, payload, created_at FROM outbox_messages WHERE published_at IS NULL;";

        await using var reader = await command.ExecuteReaderAsync();

        var outboxMessages = new List<OutboxMessage>();

        while (await reader.ReadAsync())
        {
            outboxMessages.Add(MapMessage(reader));
        }

        return outboxMessages;
    }

    static OutboxMessage MapMessage(NpgsqlDataReader reader)
    {
        return new OutboxMessage(reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetFieldValue<DateTimeOffset>(3), null );
    }

    public async Task InsertAsync(OutboxMessage outboxMessage)
    {
        await using var command = connection.CreateCommand();

        command.Transaction = transaction;
        command.CommandText = "INSERT INTO outbox_messages (message_id, type, payload, created_at) VALUES (@messageId, @type, @payload, @createdAt);";
        command.Parameters.AddWithValue("messageId", outboxMessage.MessageId);
        command.Parameters.AddWithValue("type", outboxMessage.Type);
        command.Parameters.AddWithValue("payload", outboxMessage.Payload);
        command.Parameters.AddWithValue("createdAt", outboxMessage.CreatedAt);

        await command.ExecuteNonQueryAsync();
    }

    public async Task MarkAsPublishedAsync(Guid messageId,  DateTimeOffset publishedAt, CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();

        command.Transaction = transaction;
        command.CommandText = "UPDATE outbox_messages SET published_at = @publishedAt, type='ExpensePublished' WHERE message_id = @messageId";

        command.Parameters.AddWithValue("messageId", messageId);
        command.Parameters.AddWithValue("publishedAt", publishedAt);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OutboxMessage>> ClaimPendingAsync(DateTimeOffset claimedUntil, int batchSize, CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;

        command.CommandText = """
            WITH candidates AS (
                SELECT message_id
                FROM outbox_messages
                WHERE published_at IS NULL
                AND (claimed_until IS NULL OR claimed_until < NOW())
                ORDER BY created_at
                FOR UPDATE SKIP LOCKED
                LIMIT @batchSize
            )
            UPDATE outbox_messages o
            SET claimed_until = @claimedUntil
            FROM candidates c
            WHERE o.message_id = c.message_id
            RETURNING
                o.message_id,
                o.type,
                o.payload,
                o.created_at,
                o.published_at;
            """;

        command.Parameters.AddWithValue("claimedUntil", claimedUntil);
        command.Parameters.AddWithValue("batchSize", batchSize);

        var messages = new List<OutboxMessage>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            messages.Add(new OutboxMessage(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetFieldValue<DateTimeOffset>(3),
                reader.IsDBNull(4) ? null : reader.GetFieldValue<DateTimeOffset>(4)));
        }

        return messages;
    }
}