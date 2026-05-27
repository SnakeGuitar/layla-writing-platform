namespace client_web.Models;

public class Collaborator
{
    public string UserId { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
}
