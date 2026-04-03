namespace client_web.Services.Voice.SignalR;

public interface ISignalRClient : IAsyncDisposable
{
    bool IsConnected { get; }

    Task ConnectAsync(string url, string token);
    Task DisconnectAsync();

    Task InvokeAsync(string method, params object[] args);

    void On<T>(string methodName, Action<T> handler);
    void On<T1, T2>(string methodName, Action<T1, T2> handler);

    event Action<string>? OnConnectionChanged;
}