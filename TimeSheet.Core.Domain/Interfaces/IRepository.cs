using TimeSheet.Core.Domain.SharedKernel;

namespace TimeSheet.Core.Domain.Interfaces;

/// <summary>
/// Generic repository interface for domain entities.
/// Provides common CRUD operations for all entities inheriting from <see cref="BaseEntity"/>.
/// </summary>
/// <typeparam name="T">The entity type, constrained to BaseEntity.</typeparam>
public interface IRepository<T> where T : BaseEntity
{
    /// <summary>
    /// Retrieves an entity by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The entity if found; otherwise, null.</returns>
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all entities of type T from the repository.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A read-only list of all entities.</returns>
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new entity to the repository.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity in the repository.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <remarks>
    /// This method is synchronous because EF Core's Update() only marks the entity as modified
    /// in the change tracker. The actual database update happens when SaveChangesAsync is called.
    /// </remarks>
    void Update(T entity);

    /// <summary>
    /// Removes an entity from the repository.
    /// </summary>
    /// <param name="entity">The entity to remove.</param>
    /// <remarks>
    /// This method is synchronous because EF Core's Remove() only marks the entity for deletion
    /// in the change tracker. The actual database deletion happens when SaveChangesAsync is called.
    /// </remarks>
    void Remove(T entity);
}
