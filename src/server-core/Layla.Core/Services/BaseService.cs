using Layla.Core.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Layla.Core.Services;

/// <summary>
/// Base class for all application services. Centralizes common exception handling, logging, and Result pattern usage.
///
/// Responsibilities:
/// - Wraps all service operations with try-catch to prevent unhandled exceptions from propagating to controllers
/// - Logs errors with contextual information (service name, parameters) for observability
/// - Maps technical exceptions (DbUpdateException, OperationCanceledException) to domain ErrorCodes
/// - Ensures all service methods return Result&lt;T&gt; for consistent error handling across the API
///
/// Usage:
/// Services should inherit from BaseService&lt;TService&gt; and use ExecuteAsync() for all async operations.
/// This ensures consistent exception handling and logging without try-catch boilerplate in each method.
/// </summary>
/// <typeparam name="TService">The concrete service type for typed logging context (e.g., ProjectService).</typeparam>
public abstract class BaseService<TService> where TService : class
{
    protected readonly ILogger<TService> Logger;

    protected BaseService(ILogger<TService> logger)
    {
        Logger = logger;
    }

    /// <summary>
    /// Executes an async operation with centralized exception handling and logging.
    ///
    /// This method eliminates try-catch boilerplate by:
    /// 1. Executing the provided async operation
    /// 2. Catching any exception that occurs
    /// 3. Logging the error with the provided message and context parameters
    /// 4. Mapping the exception to a domain ErrorCode via MapException()
    /// 5. Returning a failed Result containing the error code
    ///
    /// OperationCanceledException is re-thrown to propagate cancellation requests to callers.
    /// </summary>
    /// <typeparam name="T">The type of data returned on success.</typeparam>
    /// <param name="action">The async operation to execute. Should return a Result&lt;T&gt;.</param>
    /// <param name="logMessage">The error message template (e.g., "Failed to retrieve projects for user {UserId}").</param>
    /// <param name="args">Format arguments for the log message (e.g., userId).</param>
    /// <returns>The result of the operation, or a failed Result if an exception occurred.</returns>
    /// <example>
    /// public Task&lt;Result&lt;ProjectResponseDto&gt;&gt; GetProjectAsync(Guid projectId) =>
    ///     ExecuteAsync(async () =>
    ///     {
    ///         var project = await _repository.GetByIdAsync(projectId);
    ///         if (project == null)
    ///             return Result&lt;ProjectResponseDto&gt;.Failure(ErrorCode.ProjectNotFound);
    ///         return Result&lt;ProjectResponseDto&gt;.Success(MapToDto(project));
    ///     }, "Failed to retrieve project {ProjectId}", projectId);
    /// </example>
    protected async Task<Result<T>> ExecuteAsync<T>(
        Func<Task<Result<T>>> action,
        string logMessage,
        params object?[] args)
    {
        try
        {
            return await action();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, logMessage, args);
            return Result<T>.Failure(MapException(ex));
        }
    }

    /// <summary>
    /// Maps framework/infrastructure exceptions to domain error codes for API responses.
    ///
    /// This method translates low-level exceptions into domain-specific error codes that are safe to expose via the API:
    /// - DbUpdateException → DatabaseError (500) — database constraint or connection issues
    /// - OperationCanceledException → Re-thrown — allows cancellation requests to propagate (e.g., HTTP client timeout)
    /// - All other exceptions → InternalError (500) — prevents accidental information leakage
    ///
    /// Derived services can override to add custom exception mappings (e.g., IdentityException → ValidationFailed).
    /// </summary>
    /// <param name="ex">The exception that occurred during operation.</param>
    /// <returns>A domain ErrorCode suitable for an API response.</returns>
    protected virtual ErrorCode MapException(Exception ex) => ex switch
    {
        DbUpdateException => ErrorCode.DatabaseError,
        OperationCanceledException oce => throw oce,
        _ => ErrorCode.InternalError
    };
}
