using client_web.Application.Schemas.Profile;
using client_web.Models;

namespace client_web.Application.Services.Profile;

public interface IProfileService
{
    Task<UserProfile?> GetCurrentProfileAsync(CancellationToken ct = default);
    Task<(bool Success, UserProfile? Profile, string? Error)> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default);
}
