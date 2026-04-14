using System.Collections.Generic;

namespace Layla.Desktop.Models.Validation
{
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public Dictionary<string, List<string>> Errors { get; set; } = new();

        public static ValidationResult Success() => new ValidationResult { IsValid = true };

        public static ValidationResult Failure(Dictionary<string, List<string>> errors) => new ValidationResult
        {
            IsValid = false,
            Errors = errors
        };

        public void AddError(string propertyName, string errorMessage)
        {
            IsValid = false;
            if (!Errors.ContainsKey(propertyName))
            {
                Errors[propertyName] = new List<string>();
            }
            Errors[propertyName].Add(errorMessage);
        }
    }
}
