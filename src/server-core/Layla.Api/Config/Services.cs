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
        builder.Services.AddSingleton<IPublisher, Publisher>();
        builder.Services.AddSingleton<ConsumerBase>();
        builder.Services.AddCoreServices(builder.Configuration);
    }
}