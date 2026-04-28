using System;
using System.Text.RegularExpressions;

namespace Layla.Desktop.Services.Validation
{
    public static class ValidationService
    {
        private const int MaxEmailLength = 254;
        private const int MaxPasswordLength = 128;
        private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);

        private static readonly Regex EmailRegex = new(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            RegexTimeout);

        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || email.Length > MaxEmailLength) return false;

            try
            {
                return EmailRegex.IsMatch(email);
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        public static bool IsStrongPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length > MaxPasswordLength) return false;
            if (password.Length < 6) return false;

            bool hasNumber = false;
            bool hasUpper = false;
            foreach (char c in password)
            {
                if (char.IsDigit(c)) hasNumber = true;
                else if (char.IsUpper(c)) hasUpper = true;
                if (hasNumber && hasUpper) return true;
            }
            return false;
        }

        public static bool IsRequired(string value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }
    }
}
