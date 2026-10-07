using System.Text;
using Idempotency.Api.Models;
using RabbitMQ.Client;

namespace Idempotency.Api.Messaging.RabbitMq;

public sealed class RabbitMqMessagePublisher : IMessagePublisher, IAsyncDisposable
{
    private const string QueueName = "expense-created";

    private IConnection? _connection;
    private IChannel? _channel;

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_connection is not null && _channel is not null)
            return;

        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            Port = 8672,
            UserName = "guest",
            Password = "guest"
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken);

        var options = new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true);

        _channel = await _connection.CreateChannelAsync(options, cancellationToken);

        await _channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);
    }

    public async Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken);

        var body = Encoding.UTF8.GetBytes(message.Payload);

        var properties = new BasicProperties
        {
            MessageId = message.MessageId.ToString(),
            Type = message.Type,
            ContentType = "text/plain",
            DeliveryMode = DeliveryModes.Persistent
        };

        await _channel!.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: QueueName,
            mandatory: true,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
            await _channel.DisposeAsync();

        if (_connection is not null)
            await _connection.DisposeAsync();
    }
}