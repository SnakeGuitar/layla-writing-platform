using Microsoft.AspNetCore.SignalR.Client;

namespace client_web.Application.Config.SignalR;

/// <summary>
/// Contrato para clientes que interactúan con un Hub de SignalR.
/// Permite abrir/cerrar conexiones, invocar métodos remotos y registrar manejadores
/// para recibir mensajes del servidor.
/// SignalR funciona como un sistema de eventos remoto:
/// El servidor ejecuta algo → el cliente recibe → tu handler se ejecuta automáticamente
/// Piensa en SignalR como:
///   -InvokeAsync → haces una llamada
///   -SendAsync → el servidor transmite
///   -On() → tú estás escuchando una estación
/// </summary>
public interface ISignalRClient : IAsyncDisposable
{
    /// <summary>
    /// Define el Hub de SignalR.
    /// </summary>
    /// 
    HubConnection? Hub { get; }

    /// <summary>
    /// Define si el cliente está actualmente conectado al Hub de SignalR.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Evento disparado cuando cambia el estado de la conexión.
    /// Valores posibles: <see cref="ConnectionState.Connected"/>, 
    /// <see cref="ConnectionState.Reconnecting"/>, <see cref="ConnectionState.Disconnected"/>.
    /// </summary>
    event EventHandler<HubConnectionState>? OnConnectionChanged;

    /// <summary>
    /// Abre una conexión con el servidor de SignalR.
    /// </summary>
    /// <param name="url">URL absoluta del Hub de SignalR.</param>
    /// <param name="token">Token JWT para autenticación (sin prefijo "Bearer").</param>
    /// <exception cref="ArgumentNullException">Si <paramref name="url"/> o <paramref name="token"/> son nulos.</exception>
    /// <exception cref="InvalidOperationException">Si ya existe una conexión activa.</exception>
    /// <example>
    /// await client.ConnectAsync("https://server/hub", jwtToken);
    /// </example>
    Task ConnectAsync(string url, string token);

    /// <summary>
    /// Cierra la conexión activa con el servidor de SignalR.
    /// </summary>
    /// <remarks>Si no hay conexión activa, la llamada no tiene efecto.</remarks>
    Task DisconnectAsync();

    /// <summary>
    /// Invoca un método en el servidor de SignalR.
    /// </summary>
    /// <param name="method">Nombre del método remoto.</param>
    /// <param name="args">Argumentos a enviar.</param>
    /// <exception cref="InvalidOperationException">Si no existe conexión activa.</exception>
    Task InvokeSafeAsync(string method, params object[] args);

    /// <summary>
    /// Registra un manejador para un método específico que el servidor de SignalR puede invocar. 
    /// El manejador se ejecuta cada vez que el servidor llama al método registrado.
    /// </summary>
    /// <typeparam name="T">Tipo del parámetro recibido desde el servidor.</typeparam>
    /// <param name="methodName">Nombre del método en el servidor.</param>
    /// <param name="handler">Delegado que maneja la invocación.</param>
    void On<T>(string methodName, Action<T> handler);

    /// <summary>
    /// Registra un manejador para un método específico que el servidor de SignalR puede invocar, con dos parámetros. 
    /// El manejador se ejecuta cada vez que el servidor llama al método registrado.
    /// </summary>
    /// <typeparam name="T1">Tipo del primer parámetro recibido desde el servidor.</typeparam>
    /// <typeparam name="T2">Tipo del segundo parámetro recibido desde el servidor.</typeparam>
    /// <param name="methodName">Nombre del método en el servidor.</param>
    /// <param name="handler">Delegado que maneja la invocación.</param>
    void On<T1, T2>(string methodName, Action<T1, T2> handler);
}