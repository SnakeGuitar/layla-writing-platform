namespace client_web.Application.Services.Voice;

/// <summary>
/// Maneja la lógica de salas de voz (rooms) y participantes.
/// </summary>
public interface IRoomService
{
    /// <summary>
    /// Evento que emite el estado completo de la sala.
    /// </summary>
    event EventHandler<List<VoiceParticipant>>? OnRoomStateChanged;

    /// <summary>
    /// Evento cuando un usuario entra a la sala.
    /// </summary>
    event EventHandler<VoiceParticipant>? OnUserJoined;

    /// <summary>
    /// Evento cuando un usuario sale de la sala.
    /// </summary>
    event EventHandler<string>? OnUserLeft;

    /// <summary>
    /// Evento cuando un usuario comienza a hablar.
    /// </summary>
    event EventHandler<(string userId, string displayName)>? OnSpeakerStarted;

    /// <summary>
    /// Evento cuando un usuario deja de hablar.
    /// </summary>
    event EventHandler<string>? OnSpeakerStopped;

    /// <summary>
    /// Se une a una sala de voz.
    /// </summary>
    /// <param name="projectId">Identificador de la sala.</param>
    Task JoinRoomAsync(Guid projectId);

    /// <summary>
    /// Abandona una sala de voz.
    /// </summary>
    /// <param name="projectId">Identificador de la sala.</param>
    Task LeaveRoomAsync(Guid projectId);
}