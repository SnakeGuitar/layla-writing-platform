namespace client_web.Application.Schemas.Profile;

public class UpdateProfileRequest
{
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
}
