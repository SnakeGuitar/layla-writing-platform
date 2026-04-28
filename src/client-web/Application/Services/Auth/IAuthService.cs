using client_web.Application.Schemas.Auth;

namespace client_web.Application.Services.Auth;

public interface IAuthService
{
    /// <summary>
    /// Servicio para iniciar autenticación y obtener un token JWT.
    /// </summary>
    /// <param name="requestData">Datos de solicitud de inicio de sesión.</param>
    public Task<LoginResponse> LoginAsync(LoginRequest requestData);
}