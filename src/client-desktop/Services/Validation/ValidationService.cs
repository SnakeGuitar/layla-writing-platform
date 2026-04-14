using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Layla.Desktop.Services.Validation
{
    public static class ValidationService
    {
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            var pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, pattern, RegexOptions.IgnoreCase);
        }

        public static bool IsStrongPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password)) return false;

            var hasMinimumChars = new Regex(@".{6,}");
            var hasNumber = new Regex(@"[0-9]+");
            var hasUpperChar = new Regex(@"[A-Z]+");

            return hasMinimumChars.IsMatch(password) && 
                   hasNumber.IsMatch(password) && 
                   hasUpperChar.IsMatch(password);
        }

        public static bool IsRequired(string value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }
    }
}
