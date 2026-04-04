using client_web.Application.Config.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

namespace client_web.Application.Services.ActiveStatusAuthor;

public class PresenceService : IPresenceService
{
    private readonly ISignalRClient _client;
    private readonly string _baseUrl;
    private bool _handlersRegistered;

    public PresenceService(ISignalRClient client, IConfiguration configuration)
    {
        _client = client;
        _baseUrl = configuration["ApiUrls:BackendURL"]!;
        _client.OnConnectionChanged += state => ConnectionChanged?.Invoke(this, state);
    }

    // Connection -------------------------------------------------------------------
    public bool IsConnected => _client.IsConnected;
    public event EventHandler<string>? OnConnectionChanged;

    public async Task ConnectAsync(string token) => await _client.ConnectAsync($"{_baseUrl}/presenceHub", token);

    public Task DisconnectAsync() => _client.DisconnectAsync();

    public async ValueTask DisposeAsync() => await _client.DisposeAsync();

    // 

    public async Task WatchProjectAsync(Guid projectId)
    {
        if (_hub?.State != HubConnectionState.Connected) return;
        await _hub.InvokeAsync("WatchProject", projectId);
    }
}
