using System;

namespace Layla.Desktop.Models.Authentication
{
    public class AuthResponse
    {
        public string UserId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}