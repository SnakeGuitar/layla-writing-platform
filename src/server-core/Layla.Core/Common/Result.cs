namespace Layla.Core.Common;

/// <summary>
/// Generic result wrapper for operation outcomes.
/// Supports both typed ErrorCode and string error messages for backward compatibility.
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }
    public ErrorCode? ErrorCode { get; init; }

    /// <summary>Creates a successful result with data.</summary>
    public static Result<T> Success(T data) => new() { IsSuccess = true, Data = data };

    /// <summary>Creates a failed result with an error code. Message is auto-generated.</summary>
    public static Result<T> Failure(ErrorCode code) =>
        new()
        {
            IsSuccess = false,
            Error = code.GetMessage(),
            ErrorCode = code
        };

    /// <summary>Creates a failed result with an error code and custom message.</summary>
    public static Result<T> Failure(ErrorCode code, string customMessage) =>
        new()
        {
            IsSuccess = false,
            Error = customMessage,
            ErrorCode = code
        };

    /// <summary>Creates a failed result with a custom error message and optional error code (backward compatibility).</summary>
    public static Result<T> Failure(string error, ErrorCode? code = null) =>
        new()
        {
            IsSuccess = false,
            Error = error,
            ErrorCode = code
        };
}