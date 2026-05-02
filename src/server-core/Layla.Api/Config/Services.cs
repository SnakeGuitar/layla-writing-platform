using Layla.Core.Extensions;
using Layla.Core.Interfaces;
using Layla.Core.Interfaces.Queue;
using Layla.Infrastructure.Queue;
using Layla.Infrastructure.Services;

namespace Layla.Api.Config;

public static class Services
{
    public static void Configure(WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IVoiceRoomManager, VoiceRoomManager>();
        builder.Services.AddSingleton<IPresenceTracker, PresenceTracker>();
        // RabbitMQ services (Connection, Publisher, IEventPublisher adapter) are
        // registered inside AddInfrastructureServices(). ConsumerBase is abstract
        // — concrete subclasses (when added) should be registered as IHostedService.
        // server-core currently only publishes events; consumption lives in
        // server-worldbuilding (Node.js).
        builder.Services.AddCoreServices(builder.Configuration);
    }
}