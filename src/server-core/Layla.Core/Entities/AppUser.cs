using Microsoft.AspNetCore.Identity;

namespace Layla.Core.Entities
{
    public class AppUser : IdentityUser
    {
        public string? DisplayName { get; set; }
        public string? Bio { get; set; }

        /// <summary>
        /// URL or data-URI for the user's profile avatar.
        /// Null means no avatar has been set — clients should render initials instead.
        /// </summary>
        public string? AvatarUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int TokenVersion { get; set; } = 1;
    }
}
