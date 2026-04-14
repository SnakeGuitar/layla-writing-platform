using System;

namespace Layla.Desktop.Models
{
    public class Collaborator
    {
        public string UserId { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string? Email { get; set; }
        public string Role { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; }
    }

    public class InviteCollaboratorRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "READER";
    }
}
