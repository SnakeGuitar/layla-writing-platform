using Layla.Core.Interfaces.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Layla.Infrastructure.Messaging;

public class EventBus : IEventBus, IDisposable, IEventPublisher
{
    private IConnection? _connection;
    private IModel? _channel;
    private readonly ILogger<EventBus> _logger;

    public EventBus(IConfiguration configuration, ILogger<EventBus> logger)
    {
        _logger = logger;
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:HostName"] ?? "localhost",
            Port = int.TryParse(configuration["RabbitMQ:Port"], out var port) ? port : 5672,
            UserName = configuration["RabbitMQ:UserName"] ?? "guest",
            Password = configuration["RabbitMQ:Password"] ?? "guest"
        };

        try
        {
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _logger.LogInformation("EventBus connected to RabbitMQ at {HostName}:{Port}", factory.HostName, factory.Port);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to RabbitMQ at {HostName}:{Port}", factory.HostName, factory.Port);
        }
    }

    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
    {
        const string exchangeName = "worldbuilding.events";
        var routingKey = @event.GetType().Name.ToLower().Replace("event", "");

        await Task.Run(() => Publish(@event, exchangeName, routingKey), cancellationToken);
    }

    public void Publish<T>(T @event, string exchangeName, string routingKey = "") where T : class
    {
        if (_channel == null)
        {
            _logger.LogWarning("Event not published: RabbitMQ channel is unavailable. EventType={EventType}, Exchange={Exchange}, RoutingKey={RoutingKey}",
                typeof(T).Name, exchangeName, routingKey);
            return;
        }

        try
        {
            _channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Topic, durable: true);

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var message = JsonSerializer.Serialize(@event, options);
            var body = Encoding.UTF8.GetBytes(message);

            var properties = _channel.CreateBasicProperties();
            properties.ContentType = "application/json";
            properties.DeliveryMode = 2;

            _channel.BasicPublish(exchange: exchangeName,
                                 routingKey: routingKey,
                                 basicProperties: properties,
                                 body: body);

            _logger.LogDebug("Event published successfully. EventType={EventType}, Exchange={Exchange}, RoutingKey={RoutingKey}",
                typeof(T).Name, exchangeName, routingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event. EventType={EventType}, Exchange={Exchange}, RoutingKey={RoutingKey}",
                typeof(T).Name, exchangeName, routingKey);
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
