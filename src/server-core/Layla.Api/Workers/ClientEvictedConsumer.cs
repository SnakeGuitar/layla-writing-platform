using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Layla.Api.Hubs;
using Layla.Infrastructure.Queue;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Layla.Api.Workers;

public class ClientEvictedConsumer : ConsumerBase
{
    private readonly IHubContext<ManuscriptHub> _manuscriptHub;
    private readonly ILogger<ClientEvictedConsumer> _logger;
    private readonly string _queueName;

    public ClientEvictedConsumer(
        Connection connection,
        IConfiguration config,
        IHubContext<ManuscriptHub> manuscriptHub,
        ILogger<ClientEvictedConsumer> logger)
        : base(connection, config, logger)
    {
        _manuscriptHub = manuscriptHub;
        _logger = logger;
        // Unique queue name so every server-core instance receives the eviction broadcast
        _queueName = $"client-evicted-hub-{Guid.NewGuid():N}";
    }

    protected override string QueueName => _queueName;

    protected override string[] BindingKeys => new[] { "client.evicted" };

    protected override async Task HandleAsync(string routingKey, string body, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<Layla.Infrastructure.Workers.ClientEvictedEvent>(body);
        if (payload != null && !string.IsNullOrEmpty(payload.UserId))
        {
            _logger.LogInformation("Evicting user {UserId} from project {ProjectId}", payload.UserId, payload.ProjectId);
            // Send eviction message to the specific user's group
            await _manuscriptHub.Clients.Group($"user:{payload.UserId}").SendAsync("ClientEvicted", payload.ProjectId, cancellationToken: ct);
        }
    }
}
