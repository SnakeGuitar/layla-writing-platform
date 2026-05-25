using System.ComponentModel.DataAnnotations;

namespace Layla.Core.Contracts.AppUser;

public record UpdateAppUserRequestDto
{
    [MaxLength(100)]
    public string? DisplayName { get; set; }

    [MaxLength(500)]
    public string? Bio { get; set; }

    /// <summary>
    /// URL or base-64 data URI for the profile avatar.
    /// Pass <c>null</c> to leave the current value unchanged.
    /// Pass an empty string to explicitly clear the avatar.
    /// </summary>
    [MaxLength(2_000_000)]
    public string? AvatarUrl { get; set; }
}
