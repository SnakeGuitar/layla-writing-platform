using Microsoft.AspNetCore.SignalR.Client;

namespace client_web.Application.Services.ActiveStatusAuthor;

/// <summary>
/// Maneja el ciclo de vida de la conexión con el Hub de SignalR.
/// Responsable de conectar, reconectar y desconectar.
/// </summary>
public interface IConnectionService : IAsyncDisposable
{
    /// <summary>
    /// Indica si la conexión está activa.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Evento que notifica cambios en el estado de la conexión.
    /// Valores posibles: Connected, Reconnecting, Disconnected.
    /// </summary>
    event EventHandler<HubConnectionState>? OnConnectionChanged;

    /// <summary>
    /// Establece conexión con el servidor usando un JWT.
    /// </summary>
    /// <param name="token">Token de autenticación.</param>
    Task ConnectAsync(string token);

    /// <summary>
    /// Cierra la conexión activa con el servidor.
    /// </summary>
    Task DisconnectAsync();
}