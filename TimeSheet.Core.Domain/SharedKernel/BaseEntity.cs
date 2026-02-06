namespace TimeSheet.Core.Domain.SharedKernel;

/// <summary>
/// Base entity class providing unique identity for all domain entities.
/// EF Core tracks entities by their Id automatically.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Gets the unique identifier for this entity.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseEntity"/> class with a new GUID.
    /// Used when creating new entities.
    /// </summary>
    protected BaseEntity()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseEntity"/> class with a specified ID.
    /// Used for entity rehydration from persistence.
    /// </summary>
    /// <param name="id">The unique identifier for this entity.</param>
    /// <exception cref="ArgumentException">Thrown when id is Guid.Empty.</exception>
    protected BaseEntity(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Entity ID cannot be empty", nameof(id));

        Id = id;
    }
}
