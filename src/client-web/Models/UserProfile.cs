namespace client_web.Models;

public class UserProfile
{
    public string Id { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
}
