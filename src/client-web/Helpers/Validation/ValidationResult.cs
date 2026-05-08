namespace client_web.Helpers.Validation;

/// <summary>
/// Result of a validation pass — accumulates per-field errors keyed by property name.
/// Mirrors <c>Layla.Desktop.Models.Validation.ValidationResult</c> so the two clients
/// share an identical contract.
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public Dictionary<string, List<string>> Errors { get; set; } = new();

    public static ValidationResult Success() => new() { IsValid = true };

    public static ValidationResult Failure(Dictionary<string, List<string>> errors) => new()
    {
        IsValid = false,
        Errors = errors,
    };

    public void AddError(string propertyName, string errorMessage)
    {
        IsValid = false;
        if (!Errors.TryGetValue(propertyName, out var list))
        {
            list = new List<string>();
            Errors[propertyName] = list;
        }
        list.Add(errorMessage);
    }
}
