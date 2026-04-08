using client_web.Application.Config.SignalR;
using client_web.Helpers;
using Microsoft.AspNetCore.SignalR.Client;

namespace client_web.Application.Services.ActiveStatusAuthor;

public class PresenceService : IPresenceService
{
    private readonly ISignalRClient _client;
    private readonly string _baseUrl;
    private readonly ILogger<PresenceService> _logger;

    public PresenceService(ISignalRClient client, IConfiguration configuration, ILogger<PresenceService> logger)
    {
        _client = client;
        _baseUrl = configuration["ApiUrls:SignalRHubURL:PresenceServiceHub"]!;
        _client.OnConnectionChanged += (sender, state) => Notify(state);
        _logger = logger;
    }

    private bool _handlersRegistered;

    private void RegisterHandlers()
    {
        if (_handlersRegistered) return;

        _client.On<PresenceStatus>("AuthorStatusChanged", status =>
            OnAuthorStatusChanged?.Invoke(this, new AuthorStatusChangedEventArgs
            {
                ProjectId = Guid.Empty, // You can modify this to include the actual project ID if needed
                IsActive = status == PresenceStatus.WatchProject
            }));

        _handlersRegistered = true;
    }

    private HubConnectionState _state;

    public HubConnectionState State => _state;

    private void Notify(HubConnectionState state)
    {
        _state = state;
        OnConnectionChanged?.Invoke(this, state);
    }

    private async Task InvokeSafeAsync(Enum method, params object[] args)
    {
        try
        {
            string methodName = FormatData.EnumToMethodName(method);
            await _client.InvokeSafeAsync(methodName, args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error calling {method}: {ex.Message}");
        }
    }

    // Connection -------------------------------------------------------------------
    public bool IsConnected => _client.IsConnected;
    public event EventHandler<HubConnectionState>? OnConnectionChanged;

    public async Task ConnectAsync(string token)
    {
        RegisterHandlers();
        await _client.ConnectAsync(_baseUrl, token);
    }

    public Task DisconnectAsync() =>
        _client.DisconnectAsync();

    public async ValueTask DisposeAsync() =>
        await _client.DisposeAsync();

    // Status -------------------------------------------------------------------
    public event EventHandler<AuthorStatusChangedEventArgs>? OnAuthorStatusChanged;

    public async Task WatchProjectAsync(Guid projectId)
    {
        await InvokeSafeAsync(PresenceStatus.WatchProject, projectId);
    }
    public async Task UnwatchProjectAsync(Guid projectId)
    {
        await InvokeSafeAsync(PresenceStatus.UnwatchProject, projectId);
    }
}
