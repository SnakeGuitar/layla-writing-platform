namespace Layla.Core.Interfaces.Services;

public interface IEmailService
{
    Task SendVerificationEmailAsync(string toEmail, string verificationPin);
}
