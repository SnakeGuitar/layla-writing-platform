namespace Layla.Core.Interfaces.Data;

/// <summary>
/// Marker interface for repositories that support database transactions.
/// Repositories implementing this interface provide explicit transaction management.
/// </summary>
public interface ITransactionalRepository
{
    /// <summary>Begins a new database transaction.</summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>Commits the current transaction and saves changes to the database.</summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>Rolls back the current transaction, discarding any pending changes.</summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes <paramref name="operation"/> inside a retriable transaction managed by
    /// EF Core's execution strategy. Use this instead of
    /// <see cref="BeginTransactionAsync"/>/<see cref="CommitTransactionAsync"/> when the
    /// DbContext is configured with <c>EnableRetryOnFailure</c> (e.g. SqlServerRetryingExecutionStrategy),
    /// which does not support user-initiated transactions opened outside the strategy scope.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);
}
