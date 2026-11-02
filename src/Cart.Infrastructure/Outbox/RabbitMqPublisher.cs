using System.Text;
using RabbitMQ.Client;

namespace Cart.Infrastructure.Outbox;

internal sealed class RabbitMqPublisher(string connectionString) : IAsyncDisposable
{
    public const string Exchange = "cart.events";

    private IConnection? _connection;
    private IChannel? _channel;

    public async Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var channel = await GetChannelAsync(cancellationToken);

        var properties = new BasicProperties
        {
            MessageId = message.Id.ToString(),
            CorrelationId = message.CorrelationId,
            Type = message.Type,
            ContentType = "application/json",
            Persistent = true,
            Timestamp = new AmqpTimestamp(message.OccurredAt.ToUnixTimeSeconds())
        };

        await channel.BasicPublishAsync(
            Exchange, message.Type, mandatory: false, properties, Encoding.UTF8.GetBytes(message.Payload), cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
            await _channel.DisposeAsync();
        if (_connection is not null)
            await _connection.DisposeAsync();
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
            return _channel;

        var factory = new ConnectionFactory { Uri = new Uri(connectionString), ClientProvidedName = "cart-service" };
        _connection ??= await factory.CreateConnectionAsync(cancellationToken);

        var channel = await _connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            cancellationToken);
        await channel.ExchangeDeclareAsync(Exchange, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);

        return _channel = channel;
    }
}
