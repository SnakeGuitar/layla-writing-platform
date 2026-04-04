using Microsoft.AspNetCore.SignalR.Client;

namespace client_web.Services;

public record PublicProjectDto(
    Guid Id,
    string Title,
    string Synopsis,
    string LiteraryGenre,
    string? CoverImageUrl,
    DateTime UpdatedAt,
    bool IsPublic
);

public class PresenceService : IAsyncDisposable
{
    private HubConnection? _hub;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _coreBaseUrl;

    public event Action<Guid, bool>? OnAuthorStatusChanged;

    public PresenceService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _coreBaseUrl = configuration["ApiUrls:BackendURL"]!;
    }

    public async Task<List<PublicProjectDto>> GetPublicProjectsAsync()
    {
        var client = _httpClientFactory.CreateClient("ServerCore");
        try
        {
            var projects = await client.GetFromJsonAsync<List<PublicProjectDto>>("/api/projects/public");
            return projects ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task ConnectAsync()
    {
        if (_hub != null) return;

        _hub = new HubConnectionBuilder()
            .WithUrl($"{_coreBaseUrl}/hubs/presence")
            .WithAutomaticReconnect()
            .Build();

        _hub.On<Guid, bool>("AuthorStatusChanged", (projectId, isActive) =>
        {
            OnAuthorStatusChanged?.Invoke(projectId, isActive);
        });

        await _hub.StartAsync();
    }

    public async Task WatchProjectAsync(Guid projectId)
    {
        if (_hub?.State != HubConnectionState.Connected) return;
        await _hub.InvokeAsync("WatchProject", projectId);
    }

    public async ValueTask DisposeAsync()
    {
        if (_hub != null)
        {
            await _hub.DisposeAsync();
            _hub = null;
        }
    }
}
