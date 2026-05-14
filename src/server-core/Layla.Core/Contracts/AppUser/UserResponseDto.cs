namespace Layla.Core.Contracts.AppUser;

public class UserResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Mirrors ASP.NET Identity's <c>LockoutEnd</c>. When set and in the
    /// future it means the account is currently locked (banned). Exposed
    /// here so the admin UI can flag banned users without a second call.
    /// </summary>
    public DateTimeOffset? LockoutEnd { get; set; }
}
