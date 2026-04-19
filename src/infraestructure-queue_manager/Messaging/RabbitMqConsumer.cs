using System.Text;
using System.Text.Json;

namespace QueueManager.Messaging;

public sealed class RabbitMqConsumer : BackgroundService
{
    private readonly RabbitMqConnection _connection;
    private readonly IConfiguration _config;
    private readonly ILogger<RabbitMqConsumer> _logger;
    private IModel? _channel;

    public RabbitMqConsumer(
        RabbitMqConnection connection,
        IConfiguration config,
        ILogger<RabbitMqConsumer> logger)
    {
        _connection = connection;
        _config = config;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var exchange = _config["RabbitMQ:ExchangeName"]!;
        var queueName = _config["RabbitMQ:QueueName"]!;

        _channel = _connection.GetConnection().CreateModel();

        // Declara exchange y queue — idempotente
        _channel.ExchangeDeclare(exchange, ExchangeType.Topic, durable: true);
        _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);

        // Suscribe la queue a todos los eventos del exchange
        _channel.QueueBind(queueName, exchange, routingKey: "user.#");
        _channel.QueueBind(queueName, exchange, routingKey: "project.#");

        // Procesa un mensaje a la vez — evita sobrecarga
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += OnMessageReceived;

        _channel.BasicConsume(queueName, autoAck: false, consumer);

        _logger.LogInformation("Consumer RabbitMQ escuchando en '{Queue}'", queueName);

        // Mantiene el background service vivo
        stoppingToken.WaitHandle.WaitOne();
        return Task.CompletedTask;
    }

    private void OnMessageReceived(object? sender, BasicDeliverEventArgs ea)
    {
        var routingKey = ea.RoutingKey;
        var body = Encoding.UTF8.GetString(ea.Body.Span);

        try
        {
            _logger.LogInformation(
                "Mensaje recibido | RoutingKey: {Key} | Body: {Body}",
                routingKey, body);

            // Despacha según routing key
            if (routingKey.StartsWith("user.created"))
            {
                var ev = JsonSerializer.Deserialize<UserCreatedEvent>(body);
                HandleUserCreated(ev!);
            }

            // ACK — confirma procesamiento exitoso
            _channel!.BasicAck(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando mensaje '{Key}'", routingKey);

            // NACK con requeue: false — manda a dead letter queue si está configurada
            _channel!.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
        }
    }

    private void HandleUserCreated(UserCreatedEvent ev)
    {
        _logger.LogInformation(
            "Usuario creado: {Name} <{Email}>", ev.Name, ev.Email);

        // Aquí va tu lógica: enviar email, actualizar caché, etc.
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        base.Dispose();
    }
}