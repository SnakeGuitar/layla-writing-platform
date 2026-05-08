namespace Layla.Desktop.Models.Authentication
{
    public class VerifyEmailRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Pin { get; set; } = string.Empty;
    }
}
