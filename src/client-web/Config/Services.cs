using client_web.Services;
using client_web.Services.Voice.SignalR;

namespace client_web.Config;

public static class Services
{
    public static void Configure(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<ProjectService>();
        services.AddScoped<PresenceService>();
        services.AddSingleton<ISignalRClient, SignalRClient>();
        services.AddSingleton<IVoiceService, VoiceService>();
        services.AddSingleton<IVoiceConnectionService>(sp => sp.GetRequiredService<IVoiceService>());
        services.AddSingleton<IVoiceRoomService>(sp => sp.GetRequiredService<IVoiceService>());
        services.AddSingleton<IVoiceAudioService>(sp => sp.GetRequiredService<IVoiceService>());
    }
}