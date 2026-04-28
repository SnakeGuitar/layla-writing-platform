using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Layla.Infrastructure.Queue;

public sealed class Connection : IDisposable
{
    private ConnectionFactory _connectionFactory;
    private readonly ILogger<Connection> _logger;

    public Connection(IConfiguration config, ILogger<Connection> logger)
    {
        _logger = logger;

        _connectionFactory = new ConnectionFactory
        {
            HostName = config["RabbitMQ:HostName"]!,
            Port = int.Parse(config["RabbitMQ:Port"]!),
            UserName = config["RabbitMQ:Username"]!,
            Password = config["RabbitMQ:Password"]!,
            VirtualHost = config["RabbitMQ:VirtualHost"]!,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
        };
    }

    private IConnection? _connection;
    private IModel? _channel;
    private readonly object _lock = new();

    public bool IsConnected =>
        _connection is { IsOpen: true } &&
        _channel is { IsOpen: true };

    // Exposing chanel for publishers y consumers
    public IModel Channel
    {
        get
        {
            EnsureConnected();
            return _channel!;
        }
    }

    public void EnsureConnected()
    {
        lock (_lock)
        {
            if (IsConnected) return;

            try
            {
                // Clean resources
                _channel?.Dispose();
                _connection?.Dispose();

                _connection = _connectionFactory.CreateConnection();
                _channel = _connection.CreateModel();

                _logger.LogInformation(
                    "RabbitMQ connected at {HostName}:{Port}",
                    _connectionFactory.HostName, _connectionFactory.Port);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to connect to RabbitMQ at {HostName}:{Port}",
                    _connectionFactory.HostName, _connectionFactory.Port);
                throw;
            }
        }
    }

    public void Dispose()
    {
        try
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disposing RabbitMQ connection.");
        }
        finally
        {
            _channel = null;
            _connection = null;
        }
    }
}