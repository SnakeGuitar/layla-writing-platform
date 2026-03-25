using Layla.Core.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Layla.Core.Services;

/// <summary>
/// Base class for all services. Centralizes common exception handling and logging patterns.
/// </summary>
/// <typeparam name="TService">The service type for typed logging context.</typeparam>
public abstract class BaseService<TService> where TService : class
{
    protected readonly ILogger<TService> Logger;

    protected BaseService(ILogger<TService> logger)
    {
        Logger = logger;
    }

    /// <summary>
    /// Executes an async operation with centralized exception handling and logging.
    /// </summary>
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
    /// Maps framework exceptions to domain error codes.
    /// Override to add service-specific exception handling.
    /// </summary>
    protected virtual ErrorCode MapException(Exception ex) => ex switch
    {
        DbUpdateException => ErrorCode.DatabaseError,
        OperationCanceledException oce => throw oce,
        _ => ErrorCode.InternalError
    };
}
