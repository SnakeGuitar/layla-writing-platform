using Layla.Core.Extensions;
using Layla.Core.Interfaces;
using Layla.Infrastructure.Services;

namespace Layla.Api.Config;

public static class Services
{
    public static void Configure(WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IVoiceRoomManager, VoiceRoomManager>();
        builder.Services.AddSingleton<IPresenceTracker, PresenceTracker>();
        builder.Services.AddCoreServices(builder.Configuration);
    }
}