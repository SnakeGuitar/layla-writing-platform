/// <summary>
/// Facade que agrupa todos los servicios de voz.
/// Facilita el consumo desde UI sin conocer la separación interna.
/// </summary>
public interface IVoiceService :
    IVoiceConnectionService,
    IVoiceRoomService,
    IVoiceAudioService
{
}