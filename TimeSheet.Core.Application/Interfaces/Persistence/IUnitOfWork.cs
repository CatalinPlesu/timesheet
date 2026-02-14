namespace TimeSheet.Core.Application.Interfaces.Persistence;

/// <summary>
/// Unit of Work pattern interface for coordinating multiple repository operations
/// within a single database transaction.
/// </summary>
/// <remarks>
/// The Unit of Work pattern ensures that all repository operations succeed or fail together,
/// maintaining data consistency. Call CompleteAsync() to persist all changes to the database.
/// </remarks>
public interface IUnitOfWork : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Persists all changes made through repositories to the database.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The number of state entries written to the database.</returns>
    /// <remarks>
    /// This method commits the current transaction. If it fails, all changes are rolled back.
    /// </remarks>
    Task<int> CompleteAsync(CancellationToken cancellationToken = default);
}
