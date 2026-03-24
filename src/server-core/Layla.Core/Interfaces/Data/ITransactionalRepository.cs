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
}
