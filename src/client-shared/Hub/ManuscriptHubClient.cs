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

    /// <summary>Raised when another collaborator's cursor moves.</summary>
    public event Action<string, int>? CursorMoved;

    /// <summary>Raised when the current user has been evicted from a project.</summary>
    public event Action<Guid>? ClientEvicted;

    /// <summary>Raised when wiki entities change and the tokenizer should rebuild.</summary>
    public event Action? WikiEntitiesChanged;

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
        _connection.On<string, int>("OnCursorMoved", (userId, offset) =>
        {
            CursorMoved?.Invoke(userId, offset);
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
