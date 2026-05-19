using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Layla.Core.Interfaces.Data;
using Layla.Core.Interfaces.Queue;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Layla.Infrastructure.Workers;

public class OutboxProcessorWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessorWorker> _logger;

    public OutboxProcessorWorker(IServiceProvider serviceProvider, ILogger<OutboxProcessorWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(5000, stoppingToken);
        }
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        var messages = await outboxRepo.GetUnprocessedMessagesAsync(20, stoppingToken);
        var messageList = messages.ToList();

        foreach (var message in messageList)
        {
            try
            {
                if (message.EventType == "ClientEvicted")
                {
                    var payload = JsonSerializer.Deserialize<ClientEvictedEvent>(message.Payload);
                    if (payload != null)
                    {
                        publisher.Publish(payload, "client.evicted");
                    }
                }
                
                message.Processed = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process outbox message {Id}", message.Id);
            }
        }

        if (messageList.Any())
        {
            await outboxRepo.SaveChangesAsync(stoppingToken);
        }
    }
}

public class ClientEvictedEvent
{
    public Guid ProjectId { get; set; }
    public string UserId { get; set; } = string.Empty;
}
