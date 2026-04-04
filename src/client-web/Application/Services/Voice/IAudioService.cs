namespace client_web.Application.Services.Voice;

/// <summary>
/// Maneja el envío y recepción de audio en tiempo real.
/// </summary>
public interface IAudioService
{
    /// <summary>
    /// Evento que se dispara cuando se recibe audio de otro usuario.
    /// </summary>
    /// <param name="senderId">Usuario que envía el audio.</param>
    /// <param name="audioData">Datos binarios del audio.</param>
    event EventHandler<(string senderId, byte[] audio)>? OnAudioReceived;

    /// <summary>
    /// Notifica al servidor que el usuario comenzó a hablar.
    /// </summary>
    /// <param name="projectId">Identificador de la sala.</param>
    Task StartSpeakingAsync(Guid projectId);

    /// <summary>
    /// Notifica al servidor que el usuario dejó de hablar.
    /// </summary>
    /// <param name="projectId">Identificador de la sala.</param>
    Task StopSpeakingAsync(Guid projectId);

    /// <summary>
    /// Envía un fragmento de audio al servidor.
    /// </summary>
    /// <param name="projectId">Identificador de la sala.</param>
    /// <param name="audioData">Datos binarios del audio.</param>
    Task SendAudioAsync(Guid projectId, byte[] audioData);
}