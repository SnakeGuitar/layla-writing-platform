namespace Layla.Desktop.Models.User.Validation;

public class ValidationResult
{
    public bool IsValid { get; set; }
    public Dictionary<string, List<string>> Errors { get; set; } = new();

    public static ValidationResult Success() => new() { IsValid = true };

    public static ValidationResult Failure(Dictionary<string, List<string>> errors) => new()
    {
        IsValid = false,
        Errors = errors
    };

    public void AddError(string propertyName, string errorMessage)
    {
        IsValid = false;
        if (!Errors.ContainsKey(propertyName))
        {
            Errors[propertyName] = new();
        }
        Errors[propertyName].Add(errorMessage);
    }
}
