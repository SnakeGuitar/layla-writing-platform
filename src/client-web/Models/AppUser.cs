using Microsoft.AspNetCore.Identity;

namespace client_web.Models;

public class AppUser : IdentityUser
{
    public string? Bio { get; set; }
    public string? DisplayName { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TokenVersion { get; set; }
}
