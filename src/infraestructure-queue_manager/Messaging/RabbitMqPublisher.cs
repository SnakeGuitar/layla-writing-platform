using System.Text;
using System.Text.Json;

namespace QueueManager.Messaging;

public interface IRabbitMqPublisher
{
    void Publish<T>(T @event, string routingKey);
}

public sealed class RabbitMqPublisher : IRabbitMqPublisher, IDisposable
{
    private readonly IModel _channel;
    private readonly string _exchange;

    public RabbitMqPublisher(RabbitMqConnection connection, IConfiguration config)
    {
        _exchange = config["RabbitMQ:ExchangeName"]!;
        _channel = connection.GetConnection().CreateModel();

        // Declara el exchange si no existe — idempotente
        _channel.ExchangeDeclare(
            exchange: _exchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);
    }

    public void Publish<T>(T @event, string routingKey)
    {
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(@event));

        var props = _channel.CreateBasicProperties();
        props.Persistent = true;  // sobrevive reinicios de RabbitMQ
        props.ContentType = "application/json";
        props.Type = typeof(T).Name;
        props.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        props.CorrelationId = Guid.NewGuid().ToString("N");

        _channel.BasicPublish(
            exchange: _exchange,
            routingKey: routingKey,
            basicProperties: props,
            body: body);
    }

    public void Dispose() => _channel?.Dispose();
}