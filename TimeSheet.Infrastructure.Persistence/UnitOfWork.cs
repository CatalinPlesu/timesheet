using TimeSheet.Core.Application.Interfaces;

namespace TimeSheet.Infrastructure.Persistence;

/// <summary>
/// Unit of Work implementation using Entity Framework Core.
/// Coordinates multiple repository operations within a single database transaction.
/// </summary>
/// <param name="dbContext">The application database context.</param>
/// <remarks>
/// The UnitOfWork wraps the DbContext and provides a single point of control for
/// committing changes. All repository operations are tracked by the DbContext's
/// change tracker and persisted together when CompleteAsync() is called.
///
/// Future enhancement: Add repository properties here when entity-specific repositories
/// are needed (e.g., IUserRepository Users { get; }). For now, services can inject
/// IRepository&lt;T&gt; directly for generic CRUD operations.
/// </remarks>
public sealed class UnitOfWork(AppDbContext dbContext) : IUnitOfWork
{
    private bool _disposed;

    /// <inheritdoc/>
    public async Task<int> CompleteAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_disposed)
        {
            dbContext.Dispose();
            _disposed = true;
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await dbContext.DisposeAsync();
            _disposed = true;
        }
    }
}
