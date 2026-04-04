using client_web.Application.Services.Http;
using client_web.Services;
using client_web.Services.Voice.SignalR;

namespace client_web.Config;

public static class Services
{
    public static void Configure(this IServiceCollection services)
    {
        // HTTP Services
        services.AddScoped<ApiClient>();
        services.AddScoped<AuthService>();

        // Other Services
        services.AddScoped<PresenceService>();
        services.AddScoped<ProjectService>();

        // Voice Services
        services.AddSingleton<ISignalRClient, SignalRClient>();
        services.AddSingleton<IVoiceService, VoiceService>();
        services.AddSingleton<IVoiceConnectionService>(sp => sp.GetRequiredService<IVoiceService>());
        services.AddSingleton<IVoiceRoomService>(sp => sp.GetRequiredService<IVoiceService>());
        services.AddSingleton<IVoiceAudioService>(sp => sp.GetRequiredService<IVoiceService>());
    }
}