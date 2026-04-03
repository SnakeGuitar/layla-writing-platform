using Microsoft.AspNetCore.SignalR.Client;

namespace client_web.Services.Voice.SignalR;

public class SignalRClient : ISignalRClient
{
    private HubConnection? _hub;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    public bool IsConnected => _hub?.State == HubConnectionState.Connected;

    public event Action<string>? OnConnectionChanged;

    public async Task ConnectAsync(string url, string token)
    {
        if (IsConnected) return;

        await _connectionLock.WaitAsync();
        try
        {
            if (IsConnected) return;

            _hub = new HubConnectionBuilder()
                .WithUrl(url, options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                })
                .WithAutomaticReconnect()
                .Build();

            _hub.Closed += _ =>
            {
                OnConnectionChanged?.Invoke("Disconnected");
                return Task.CompletedTask;
            };

            _hub.Reconnecting += _ =>
            {
                OnConnectionChanged?.Invoke("Reconnecting");
                return Task.CompletedTask;
            };

            _hub.Reconnected += _ =>
            {
                OnConnectionChanged?.Invoke("Connected");
                return Task.CompletedTask;
            };

            await _hub.StartAsync();
            OnConnectionChanged?.Invoke("Connected");
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async Task DisconnectAsync()
    {
        await _connectionLock.WaitAsync();
        try
        {
            if (_hub == null) return;

            await _hub.StopAsync();
            await _hub.DisposeAsync();
            _hub = null;

            OnConnectionChanged?.Invoke("Disconnected");
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async Task InvokeAsync(string method, params object[] args)
    {
        if (!IsConnected || _hub == null) return;

        await _hub.InvokeAsync(method, args);
    }

    public void On<T>(string methodName, Action<T> handler)
    {
        if (_hub == null) throw new InvalidOperationException("Call ConnectAsync first");
        _hub.On(methodName, handler);
    }

    public void On<T1, T2>(string methodName, Action<T1, T2> handler)
    {
        if (_hub == null) throw new InvalidOperationException("Call ConnectAsync first");
        _hub.On(methodName, handler);
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        _connectionLock.Dispose();
    }
}
