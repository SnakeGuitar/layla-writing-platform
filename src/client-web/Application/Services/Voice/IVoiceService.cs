namespace client_web.Application.Services.Voice;

enum AudioState
{
    StartSpeaking,
    StopSpeaking,
    SendingAudio
}

enum RoomAccessState
{
    JoinRoom,
    LeaveRoom
}

/// <summary>
/// Facade que agrupa todos los servicios de voz.
/// Facilita el consumo desde UI sin conocer la separación interna.
/// </summary>
public interface IVoiceService :
    IConnectionService,
    IRoomService,
    IAudioService
{ }