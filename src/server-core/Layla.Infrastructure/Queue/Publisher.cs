using System.Text.Json;
using Layla.Core.Interfaces.Queue;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Layla.Infrastructure.Queue;

public sealed class Publisher : IPublisher
{
    private readonly string _exchange;
    private readonly Connection _connection;
    private IModel _model;
    private ILogger<Publisher> _logger;

    public Publisher(Connection connection, IConfiguration config, ILogger<Publisher> logger)
    {
        _exchange = config["RabbitMQ:Exchange"]!;
        connection.EnsureConnected();
        _connection = connection;
        _model = connection.Channel;
        _model.ExchangeDeclare(
            exchange: _exchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);
        _logger = logger;
    }

    public void Publish<T>(T @event, string routingKey)
    {
        _connection.EnsureConnected();
        _model = _connection.Channel;

        byte[]? body = JsonSerializer.SerializeToUtf8Bytes(@event);

        IBasicProperties? props = _model.CreateBasicProperties();
        props.Persistent = true;
        props.ContentType = "application/json";
        props.Type = typeof(T).Name;
        props.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        props.CorrelationId = Guid.NewGuid().ToString("N");
        props.DeliveryMode = 2;

        _model.BasicPublish(
            exchange: _exchange,
            routingKey: routingKey,
            basicProperties: props,
            body: body);

        _logger.LogDebug(
            "Publishing '{Type}' on exchange '{Exchange}' with key '{Key}'",
            typeof(T).Name, _exchange, routingKey);
    }
}