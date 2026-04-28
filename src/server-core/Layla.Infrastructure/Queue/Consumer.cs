using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Layla.Infrastructure.Queue;

public abstract class ConsumerBase : BackgroundService
{
    private readonly string _exchange;
    private readonly Connection _connection;
    private IModel _model;
    private readonly ILogger<ConsumerBase> _logger;

    protected ConsumerBase(Connection connection, IConfiguration config, ILogger<ConsumerBase> logger)
    {
        _exchange = config["RabbitMQ:Exchange"]!;
        connection.EnsureConnected();
        _connection = connection;
        _model = connection.Channel;
        _logger = logger;
    }

    // Each consumer declare each routing keys
    protected abstract string QueueName { get; }
    protected abstract string[] BindingKeys { get; }
    protected abstract Task HandleAsync(string routingKey, string body, CancellationToken ct);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _connection.EnsureConnected();
        _model = _connection.Channel;

        _model.ExchangeDeclare(_exchange, ExchangeType.Topic, durable: true);
        _model.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false);

        foreach (string? key in BindingKeys)
            _model.QueueBind(QueueName, _exchange, key);

        // prefetchCount: 1 → Process each message per consumer
        _model.BasicQos(0, prefetchCount: 1, global: false);

        AsyncEventingBasicConsumer? consumer = new AsyncEventingBasicConsumer(_model);
        consumer.Received += async (_, ea) =>
        {
            string? body = Encoding.UTF8.GetString(ea.Body.Span);
            string? routingKey = ea.RoutingKey;
            try
            {
                await HandleAsync(routingKey, body, stoppingToken);
                _model.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (OperationCanceledException)
            {
                // Clean shutdown: requeue for another consumer to take it
                _model.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesing '{Key}'", routingKey);
                _model.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        _model.BasicConsume(QueueName, autoAck: false, consumer);
        _logger.LogInformation("Consumer listening on '{Queue}'", QueueName);

        await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
    }
}