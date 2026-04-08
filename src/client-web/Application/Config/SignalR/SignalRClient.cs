using Microsoft.AspNetCore.SignalR.Client;
using Polly;
using Polly.Retry;

namespace client_web.Application.Config.SignalR;

public class SignalRClient : ISignalRClient
{
    private string _baseUrl;
    private AsyncRetryPolicy _retryPolicy;
    private readonly ILogger<SignalRClient> _logger;

    public SignalRClient(IConfiguration configuration, ILogger<SignalRClient> logger)
    {
        _baseUrl = configuration["ApiUrls:BackendURL"]!;
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                3,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (ex, time) =>
                {
                    Console.WriteLine($"Retrying in {time}. Error: {ex.Message}");
                });
        _logger = logger;
    }

    // TODO-Desarrollo: Verfidy access method
    public HubConnection? Hub { set; get; }
    public bool IsConnected => Hub?.State == HubConnectionState.Connected;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    public event EventHandler<HubConnectionState>? OnConnectionChanged;

    public async Task ConnectAsync(string url, string token)
    {
        if (Hub != null && IsConnected) return;

        await _connectionLock.WaitAsync();
        try
        {
            if (IsConnected && Hub != null && Hub.State == HubConnectionState.Connected) return;
            await Hub!.StopAsync();
            await Hub!.DisposeAsync();

            Hub = new HubConnectionBuilder()
                .WithUrl(_baseUrl + url, options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                })
                .WithAutomaticReconnect()
                .Build();

            Hub.Reconnecting += _ =>
            {
                _logger.LogWarning("SignalR connection lost. Attempting to reconnect...");
                Notify(HubConnectionState.Reconnecting);
                return Task.CompletedTask;
            };

            Hub.Reconnected += _ =>
            {
                _logger.LogInformation("SignalR reconnected.");
                Notify(HubConnectionState.Connected);
                return Task.CompletedTask;
            };

            Hub.Closed += _ =>
            {
                _logger.LogInformation("SignalR connection closed.");
                Notify(HubConnectionState.Disconnected);
                return Task.CompletedTask;
            };

            Notify(HubConnectionState.Connecting);

            await _retryPolicy.ExecuteAsync(async () =>
            {
                _logger.LogInformation("Attempting to connect to SignalR hub...");
                await Hub.StartAsync();
            });

            Notify(HubConnectionState.Connected);
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
            if (Hub == null) return;

            _logger.LogInformation("Disconnecting from SignalR hub...");
            await Hub.StopAsync();
            await Hub.DisposeAsync();
            Hub = null;

            OnConnectionChanged?.Invoke(this, HubConnectionState.Disconnected);
        }
        finally
        {
            _logger.LogInformation("SignalR disconnect process completed.");
            _connectionLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation("Disposing SignalR client...");
        await DisconnectAsync();
        _connectionLock.Dispose();
    }

    private void Notify(HubConnectionState state)
    {
        try
        {
            OnConnectionChanged?.Invoke(this, state);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error in event handler: {ex.Message}");
        }
    }

    public async Task InvokeSafeAsync(string method, params object[] args)
    {
        if (!IsConnected || Hub == null) return;
        await Hub.InvokeAsync(method, args);
    }

    public void On<T>(string methodName, Action<T> handler)
    {
        if (Hub == null) throw new InvalidOperationException("Call ConnectAsync first");
        Hub.On(methodName, handler);
    }

    public void On<T1, T2>(string methodName, Action<T1, T2> handler)
    {
        if (Hub == null) throw new InvalidOperationException("Call ConnectAsync first");
        Hub.On(methodName, handler);
    }
}
