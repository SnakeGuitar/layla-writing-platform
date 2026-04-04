using System.Data;
using Microsoft.AspNetCore.SignalR.Client;
using Polly;
using Polly.Retry;

namespace client_web.Application.Config.SignalR;

public class SignalRClient : ISignalRClient
{
    public HubConnection? _hub;
    public readonly SemaphoreSlim _connectionLock = new(1, 1);
    public AsyncRetryPolicy _retryPolicy;
    public bool IsConnected => _hub?.State == HubConnectionState.Connected;
    public event EventHandler<ConnectionState>? OnConnectionChanged;

    public SignalRClient()
    {
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                3,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (ex, time) =>
                {
                    Console.WriteLine($"Retrying in {time}. Error: {ex.Message}");
                });
    }

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
                OnConnectionChanged?.Invoke(this, ConnectionState.Closed);
                return Task.CompletedTask;
            };

            _hub.Reconnecting += _ =>
            {
                OnConnectionChanged?.Invoke(this, ConnectionState.Connecting);
                return Task.CompletedTask;
            };

            _hub.Reconnected += _ =>
            {
                OnConnectionChanged?.Invoke(this, ConnectionState.Open);
                return Task.CompletedTask;
            };

            await _retryPolicy.ExecuteAsync(async () =>
            {
                await _hub.StartAsync();
                OnConnectionChanged?.Invoke(this, ConnectionState.Connecting);
            });
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

            OnConnectionChanged?.Invoke(this, ConnectionState.Closed);
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async Task InvokeSafeAsync(string method, params object[] args)
    {
        if (!IsConnected || _hub == null) return;

        await _retryPolicy.ExecuteAsync(async () =>
            await _hub.InvokeAsync(method, args));
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
