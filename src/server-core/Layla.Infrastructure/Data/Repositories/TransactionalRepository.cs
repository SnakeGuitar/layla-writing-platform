using Layla.Core.Interfaces.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Layla.Infrastructure.Data.Repositories;

/// <summary>
/// Base class for repositories that require explicit transaction management.
/// Provides reusable transaction handling logic for derived repositories.
/// </summary>
public abstract class TransactionalRepository : ITransactionalRepository
{
    protected readonly ApplicationDbContext DbContext;
    protected IDbContextTransaction? CurrentTransaction;

    protected TransactionalRepository(ApplicationDbContext dbContext)
    {
        DbContext = dbContext;
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (CurrentTransaction != null)
            throw new InvalidOperationException(
                "A transaction is already in progress. Nested transactions are not supported.");

        CurrentTransaction = await DbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (CurrentTransaction == null)
            throw new InvalidOperationException("No active transaction to commit.");

        try
        {
            await DbContext.SaveChangesAsync(cancellationToken);
            await CurrentTransaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await CurrentTransaction!.DisposeAsync();
            CurrentTransaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (CurrentTransaction == null)
            return;

        try
        {
            await CurrentTransaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await CurrentTransaction!.DisposeAsync();
            CurrentTransaction = null;
        }
    }

    /// <inheritdoc/>
    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
    {
        // CreateExecutionStrategy() returns the configured strategy (e.g. SqlServerRetryingExecutionStrategy).
        // Wrapping the transaction inside ExecuteAsync allows the strategy to retry the entire
        // unit-of-work on transient failures without conflicting with the retry policy.
        var strategy = DbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await DbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await operation(cancellationToken);
                await DbContext.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }
}
