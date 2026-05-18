using System.Net.Mail;
using System.Text.RegularExpressions;

namespace Layla.Desktop.Services.User.Validation;

public static class ValidationService
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);
    public static bool IsRequired(string value) =>
        !string.IsNullOrWhiteSpace(value);


    private const int MAX_EMAIL_LENGTH = 254;
    private static readonly Regex EMAIL_REGEX = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, RegexTimeout);
    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) ||
            email.Length > MAX_EMAIL_LENGTH)
            return false;

        try
        {
            MailAddress? tryParseEmail = new MailAddress(email);

            return (EMAIL_REGEX.IsMatch(email) && tryParseEmail.Address == email);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    private const int MIN_PASSWORD_LENGTH = 6;
    private const int MAX_PASSWORD_LENGTH = 128;
    public static bool IsStrongPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) ||
            password.Length > MAX_PASSWORD_LENGTH ||
            password.Length < MIN_PASSWORD_LENGTH)
            return false;

        bool hasNumber = false;
        bool hasUpper = false;
        foreach (char c in password)
        {
            if (char.IsDigit(c)) hasNumber = true;
            if (char.IsUpper(c)) hasUpper = true;
            if (hasNumber && hasUpper) return true;

            if (hasUpper && hasNumber) return true;
        }
        return false;
    }
}
