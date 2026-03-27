namespace Layla.Core.Common;

/// <summary>
/// Generic result wrapper for operation outcomes.
/// Encapsulates success with typed data or failure with a domain ErrorCode and error message.
/// All failures include a typed ErrorCode for consistent error handling and HTTP status mapping.
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }
    public ErrorCode? ErrorCode { get; init; }

    /// <summary>Creates a successful result with data.</summary>
    public static Result<T> Success(T data) => new() { IsSuccess = true, Data = data };

    /// <summary>
    /// Creates a failed result with an error code and auto-generated user-friendly message.
    /// Use when the error message from ErrorCode.GetMessage() is sufficient.
    /// </summary>
    public static Result<T> Failure(ErrorCode code) =>
        new()
        {
            IsSuccess = false,
            Error = code.GetMessage(),
            ErrorCode = code
        };

    /// <summary>
    /// Creates a failed result with an error code and a custom error message.
    /// Use when you need to provide additional context or a more specific message.
    /// Example: Failure(ErrorCode.ValidationFailed, "Email must be a valid address.")
    /// </summary>
    public static Result<T> Failure(ErrorCode code, string customMessage) =>
        new()
        {
            IsSuccess = false,
            Error = customMessage,
            ErrorCode = code
        };
}