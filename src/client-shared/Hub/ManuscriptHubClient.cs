using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace Layla.Client.Shared.Hub;

/// <summary>
/// Strongly-typed wrapper around the SignalR <see cref="HubConnection"/> for the
/// <c>/hubs/manuscript</c> endpoint.  Exposes pure C# events so WPF and Blazor
/// can subscribe without taking a direct dependency on SignalR internals.
///
/// <para>Handles automatic reconnection with exponential back-off.</para>
/// </summary>
public sealed class ManuscriptHubClient : IAsyncDisposable
{
    private HubConnection? _connection;
    private readonly ILogger<ManuscriptHubClient>? _logger;

    // ── Events ──────────────────────────────────────────────────────────

    /// <summary>Raised when another collaborator's cursor moves. Args: userId, displayName, offset.</summary>
    public event Action<string, string, int>? CursorMoved;

    /// <summary>Raised when a collaborator disconnects and their cursor should be removed. Arg: userId.</summary>
    public event Action<string>? CursorRemoved;

    /// <summary>
    /// Raised when another collaborator broadcasts a keystroke in real time.
    /// Args: userId, rtfContent. Applied directly to the editor — no API round-trip.
    /// </summary>
    public event Action<string, string>? TextChanged;

    /// <summary>Raised when the current user has been evicted from a project.</summary>
    public event Action<Guid>? ClientEvicted;

    /// <summary>Raised when wiki entities change and the tokenizer should rebuild.</summary>
    public event Action? WikiEntitiesChanged;

    /// <summary>
    /// Raised when another collaborator has saved the active chapter.
    /// Receivers should reload chapter content from the API.
    /// </summary>
    public event Action<Guid, string>? ChapterSaved;

    /// <summary>Raised when a collaborator creates a milestone. Receivers should reload version history.</summary>
    public event Action<Guid, string>? MilestoneCreated;

    /// <summary>Raised when the underlying connection state changes.</summary>
    public event Action<HubConnectionState>? ConnectionStateChanged;

    // ── Construction ────────────────────────────────────────────────────

    public ManuscriptHubClient(ILogger<ManuscriptHubClient>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>Current connection state.</summary>
    public HubConnectionState State => _connection?.State ?? HubConnectionState.Disconnected;

    /// <summary>
    /// Builds and starts the SignalR connection to the manuscript hub.
    /// </summary>
    /// <param name="hubUrl">Full URL to <c>/hubs/manuscript</c>.</param>
    /// <param name="accessTokenProvider">Delegate that returns the current JWT.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task ConnectAsync(string hubUrl, Func<Task<string?>> accessTokenProvider, CancellationToken cancellationToken = default)
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync();
        }

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = accessTokenProvider;
                // Bypass TLS certificate validation for development environments
                // that use a self-signed certificate on the server-core HTTPS endpoint.
                // This mirrors the same pattern used in ProjectApiService for the presence hub.
                options.HttpMessageHandlerFactory = handler =>
                {
                    if (handler is System.Net.Http.HttpClientHandler clientHandler)
                    {
                        clientHandler.ServerCertificateCustomValidationCallback =
                            (message, cert, chain, errors) => true;
                    }
                    else if (handler is System.Net.Http.SocketsHttpHandler socketsHandler)
                    {
                        socketsHandler.SslOptions.RemoteCertificateValidationCallback =
                            (sender, cert, chain, errors) => true;
                    }
                    return handler;
                };
            })
            .WithAutomaticReconnect(new[]
            {
                TimeSpan.Zero,
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(30)
            })
            .Build();

        // Wire server → client events
        _connection.On<string, string, int>("OnCursorMoved", (userId, displayName, offset) =>
        {
            CursorMoved?.Invoke(userId, displayName, offset);
        });

        _connection.On<string>("OnCursorRemoved", userId =>
        {
            CursorRemoved?.Invoke(userId);
        });

        _connection.On<Guid>("ClientEvicted", projectId =>
        {
            _logger?.LogWarning("Evicted from project {ProjectId}", projectId);
            ClientEvicted?.Invoke(projectId);
        });

        _connection.On("WikiEntitiesChanged", () =>
        {
            WikiEntitiesChanged?.Invoke();
        });

        _connection.On<Guid, string>("OnChapterSaved", (projectId, chapterId) =>
        {
            ChapterSaved?.Invoke(projectId, chapterId);
        });

        _connection.On<string, string>("OnTextChanged", (userId, rtfContent) =>
        {
            TextChanged?.Invoke(userId, rtfContent);
        });

        _connection.On<Guid, string>("OnMilestoneCreated", (projectId, chapterId) =>
        {
            MilestoneCreated?.Invoke(projectId, chapterId);
        });

        _connection.Reconnecting += error =>
        {
            _logger?.LogWarning(error, "ManuscriptHub reconnecting...");
            ConnectionStateChanged?.Invoke(HubConnectionState.Reconnecting);
            return Task.CompletedTask;
        };

        _connection.Reconnected += connectionId =>
        {
            _logger?.LogInformation("ManuscriptHub reconnected: {ConnectionId}", connectionId);
            ConnectionStateChanged?.Invoke(HubConnectionState.Connected);
            return Task.CompletedTask;
        };

        _connection.Closed += error =>
        {
            _logger?.LogWarning(error, "ManuscriptHub connection closed.");
            ConnectionStateChanged?.Invoke(HubConnectionState.Disconnected);
            return Task.CompletedTask;
        };

        await _connection.StartAsync(cancellationToken);
        _logger?.LogInformation("ManuscriptHub connected.");
        ConnectionStateChanged?.Invoke(HubConnectionState.Connected);
    }

    // ── Client → Server methods ─────────────────────────────────────────

    /// <summary>Joins the SignalR group for a specific chapter.</summary>
    public async Task JoinChapterGroupAsync(Guid projectId, string chapterId, CancellationToken ct = default)
    {
        EnsureConnected();
        await _connection!.InvokeAsync("JoinChapterGroup", projectId, chapterId, ct);
    }

    /// <summary>Leaves the SignalR group for a specific chapter.</summary>
    public async Task LeaveChapterGroupAsync(Guid projectId, string chapterId, CancellationToken ct = default)
    {
        EnsureConnected();
        await _connection!.InvokeAsync("LeaveChapterGroup", projectId, chapterId, ct);
    }

    /// <summary>Broadcasts the current user's cursor position to collaborators.</summary>
    public async Task SendCursorMovedAsync(Guid projectId, string chapterId, int positionOffset, CancellationToken ct = default)
    {
        EnsureConnected();
        await _connection!.InvokeAsync("SendCursorMoved", projectId, chapterId, positionOffset, ct);
    }

    /// <summary>Notifies collaborators that a milestone was just created so they reload version history.</summary>
    public async Task NotifyMilestoneCreatedAsync(Guid projectId, string chapterId, CancellationToken ct = default)
    {
        if (_connection?.State != HubConnectionState.Connected) return; // best-effort
        await _connection!.InvokeAsync("NotifyMilestoneCreated", projectId, chapterId, ct);
    }

    /// <summary>
    /// Broadcasts the current RTF content to collaborators in real time (~350 ms debounce).
    /// Receivers apply the content directly without an API round-trip.
    /// </summary>
    public async Task SendTextChangedAsync(Guid projectId, string chapterId, string rtfContent, CancellationToken ct = default)
    {
        if (_connection?.State != HubConnectionState.Connected) return; // best-effort
        await _connection!.InvokeAsync("BroadcastTextChanged", projectId, chapterId, rtfContent, ct);
    }

    /// <summary>
    /// Notifies other collaborators in the same chapter that the content has
    /// been saved, so their clients can reload the latest version.
    /// </summary>
    public async Task NotifyChapterSavedAsync(Guid projectId, string chapterId, CancellationToken ct = default)
    {
        if (_connection?.State != HubConnectionState.Connected) return; // best-effort
        await _connection!.InvokeAsync("NotifyChapterSaved", projectId, chapterId, ct);
    }

    // ── Lifecycle ───────────────────────────────────────────────────────

    /// <summary>Gracefully disconnects from the hub.</summary>
    public async Task DisconnectAsync()
    {
        if (_connection != null)
        {
            await _connection.StopAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    private void EnsureConnected()
    {
        if (_connection?.State != HubConnectionState.Connected)
            throw new InvalidOperationException(
                $"ManuscriptHubClient is not connected (state: {_connection?.State}).");
    }
}
