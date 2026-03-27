namespace Layla.Core.Common;

/// <summary>
/// Standard error codes for API responses.
/// Used in Result&lt;T&gt; to enable type-safe error handling and consistent HTTP status mapping.
/// </summary>
public enum ErrorCode
{
    // Validation errors → HTTP 400
    ValidationFailed = 1,
    InvalidInput = 2,
    InvalidRole = 3,

    // Conflict errors → HTTP 409
    DuplicateEmail = 4,
    AlreadyExists = 5,
    AlreadyMember = 6,

    // Authentication errors → HTTP 401
    Unauthorized = 101,
    InvalidCredentials = 102,
    SessionExpired = 103,
    InvalidToken = 104,

    // Account locked → HTTP 423
    AccountLocked = 105,

    // Authorization errors → HTTP 403
    Forbidden = 201,
    InsufficientPermissions = 202,

    // Not found errors → HTTP 404
    NotFound = 301,
    ProjectNotFound = 302,
    UserNotFound = 303,
    CollaboratorNotFound = 304,

    // Server errors → HTTP 500
    InternalError = 501,
    DatabaseError = 502,
    MessagingError = 503,
}

/// <summary>
/// Maps ErrorCode enum values to HTTP status codes and user-friendly messages.
/// </summary>
public static class ErrorCodeExtensions
{
    /// <summary>Gets the HTTP status code for an error code.</summary>
    public static int GetStatusCode(this ErrorCode code) => code switch
    {
        // Validation → 400
        ErrorCode.ValidationFailed or ErrorCode.InvalidInput or ErrorCode.InvalidRole => 400,

        // Conflict → 409
        ErrorCode.DuplicateEmail or ErrorCode.AlreadyExists or ErrorCode.AlreadyMember => 409,

        // Authentication → 401
        ErrorCode.Unauthorized or ErrorCode.InvalidCredentials or
        ErrorCode.SessionExpired or ErrorCode.InvalidToken => 401,

        // Account locked → 423
        ErrorCode.AccountLocked => 423,

        // Authorization → 403
        ErrorCode.Forbidden or ErrorCode.InsufficientPermissions => 403,

        // Not found → 404
        ErrorCode.NotFound or ErrorCode.ProjectNotFound or
        ErrorCode.UserNotFound or ErrorCode.CollaboratorNotFound => 404,

        // Server → 500
        ErrorCode.InternalError or ErrorCode.DatabaseError or
        ErrorCode.MessagingError => 500,

        _ => 500,
    };

    /// <summary>Gets a user-friendly error message.</summary>
    public static string GetMessage(this ErrorCode code) => code switch
    {
        ErrorCode.ValidationFailed => "Validation failed. Please check your input.",
        ErrorCode.InvalidInput => "Invalid input provided.",
        ErrorCode.DuplicateEmail => "Email is already registered.",
        ErrorCode.InvalidRole => "Invalid role specified.",

        ErrorCode.Unauthorized => "Unauthorized. Please login.",
        ErrorCode.InvalidCredentials => "Invalid email or password.",
        ErrorCode.AccountLocked => "Account is locked due to multiple failed attempts.",
        ErrorCode.SessionExpired => "Session expired. User logged in from another device.",
        ErrorCode.InvalidToken => "Invalid or malformed token.",

        ErrorCode.Forbidden => "Access denied.",
        ErrorCode.InsufficientPermissions => "You do not have permission to perform this action.",

        ErrorCode.ProjectNotFound => "Project not found.",
        ErrorCode.UserNotFound => "User not found.",
        ErrorCode.CollaboratorNotFound => "Collaborator not found.",

        ErrorCode.AlreadyExists => "Resource already exists.",
        ErrorCode.AlreadyMember => "You are already a member of this project.",

        ErrorCode.InternalError => "An internal error occurred. Please try again later.",
        ErrorCode.DatabaseError => "A database error occurred. Please try again later.",
        ErrorCode.MessagingError => "A messaging error occurred. Please try again later.",

        _ => "An unknown error occurred.",
    };
}
