using Layla.Core.Constants;
using Layla.Core.Interfaces.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Layla.Infrastructure.Messaging;

public class EventBus : IEventBus, IDisposable, IEventPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private IConnection? _connection;
    private IModel? _channel;
    private readonly ConnectionFactory _factory;
    private readonly ILogger<EventBus> _logger;
    private readonly object _connectionLock = new();

    public EventBus(IConfiguration configuration, ILogger<EventBus> logger)
    {
        _logger = logger;
        _factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:HostName"] ?? "localhost",
            Port = int.TryParse(configuration["RabbitMQ:Port"], out var port) ? port : 5672,
            UserName = configuration["RabbitMQ:UserName"] ?? "guest",
            Password = configuration["RabbitMQ:Password"] ?? "guest"
        };

        TryConnect();
    }

    private void TryConnect()
    {
        DisposeCurrentConnection();

        try
        {
            _connection = _factory.CreateConnection();
            _channel = _connection.CreateModel();
            _logger.LogInformation("EventBus connected to RabbitMQ at {HostName}:{Port}", _factory.HostName, _factory.Port);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to RabbitMQ at {HostName}:{Port}", _factory.HostName, _factory.Port);
        }
    }

    private void DisposeCurrentConnection()
    {
        try
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disposing current RabbitMQ connection.");
        }
        finally
        {
            _channel = null;
            _connection = null;
        }
    }

    public async Task<bool> PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
    {
        var exchangeName = MessagingConstants.WorldbuildingExchange;
        var typeName = @event.GetType().Name;
        if (typeName.EndsWith("Event", StringComparison.OrdinalIgnoreCase))
            typeName = typeName[..^"Event".Length];
        var routingKey = Regex.Replace(typeName, "(?<!^)([A-Z])", ".$1").ToLower();

        try
        {
            return await Task.Run(() => Publish(@event, exchangeName, routingKey), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Async publish failed. EventType={EventType}", typeof(T).Name);
            return false;
        }
    }

    public bool Publish<T>(T @event, string exchangeName, string routingKey = "") where T : class
    {
        lock (_connectionLock)
        {
            if (_channel == null || !_channel.IsOpen)
            {
                _logger.LogWarning("RabbitMQ channel unavailable, attempting reconnect. EventType={EventType}", typeof(T).Name);
                TryConnect();
            }

            if (_channel == null)
            {
                _logger.LogWarning("Event not published: RabbitMQ channel is unavailable. EventType={EventType}, Exchange={Exchange}, RoutingKey={RoutingKey}",
                    typeof(T).Name, exchangeName, routingKey);
                return false;
            }

            try
            {
                _channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Topic, durable: true);

                var message = JsonSerializer.Serialize(@event, JsonOptions);
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
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish event. EventType={EventType}, Exchange={Exchange}, RoutingKey={RoutingKey}",
                    typeof(T).Name, exchangeName, routingKey);
                return false;
            }
        }
    }

    public void Dispose()
    {
        DisposeCurrentConnection();
    }
}
