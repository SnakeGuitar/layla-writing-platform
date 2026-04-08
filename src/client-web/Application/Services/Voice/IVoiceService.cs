using Microsoft.AspNetCore.SignalR.Client;

namespace client_web.Application.Services.Voice;
/// <summary>
/// Facade que agrupa todos los servicios de voz.
/// Facilita el consumo desde UI sin conocer la separación interna.
/// </summary>
public interface IVoiceService :
    IConnectionService,
    IRoomService,
    IAudioService
{ }