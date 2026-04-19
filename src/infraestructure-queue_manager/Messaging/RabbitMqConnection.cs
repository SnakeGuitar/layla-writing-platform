namespace QueueManager.Messaging;

public sealed class RabbitMqConnection : IDisposable
{
    private readonly IConfiguration _config;
    private readonly ILogger<RabbitMqConnection> _logger;
    private IConnection? _connection;
    private readonly object _lock = new();

    public RabbitMqConnection(IConfiguration config, ILogger<RabbitMqConnection> logger)
    {
        _config = config;
        _logger = logger;
    }

    public IConnection GetConnection()
    {
        lock (_lock)
        {
            if (_connection is { IsOpen: true })
                return _connection;

            _logger.LogInformation("Conectando a RabbitMQ...");

            var factory = new ConnectionFactory
            {
                HostName = _config["RabbitMQ:Host"]!,
                Port = int.Parse(_config["RabbitMQ:Port"]!),
                UserName = _config["RabbitMQ:Username"]!,
                Password = _config["RabbitMQ:Password"]!,
                VirtualHost = _config["RabbitMQ:VirtualHost"]!,

                // Reconexión automática ante caídas de red
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
            };

            _connection = factory.CreateConnection();
            _logger.LogInformation("Conexión a RabbitMQ establecida");
            return _connection;
        }
    }

    public void Dispose() => _connection?.Dispose();
}