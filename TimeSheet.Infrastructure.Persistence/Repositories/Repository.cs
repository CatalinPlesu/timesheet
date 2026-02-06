using Microsoft.EntityFrameworkCore;
using TimeSheet.Core.Domain.Interfaces;
using TimeSheet.Core.Domain.SharedKernel;

namespace TimeSheet.Infrastructure.Persistence.Repositories;

/// <summary>
/// Generic repository implementation using Entity Framework Core.
/// Provides common CRUD operations for all entities inheriting from <see cref="BaseEntity"/>.
/// </summary>
/// <typeparam name="T">The entity type, constrained to BaseEntity.</typeparam>
/// <param name="dbContext">The application database context.</param>
public class Repository<T>(AppDbContext dbContext) : IRepository<T> where T : BaseEntity
{
    /// <summary>
    /// Gets the DbSet for the entity type.
    /// </summary>
    protected readonly DbSet<T> DbSet = dbContext.Set<T>();

    /// <inheritdoc/>
    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync([id], cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc/>
    public void Update(T entity)
    {
        DbSet.Update(entity);
    }

    /// <inheritdoc/>
    public void Remove(T entity)
    {
        DbSet.Remove(entity);
    }
}
