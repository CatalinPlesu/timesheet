namespace TimeSheet.Core.Domain.SharedKernel;

/// <summary>
/// Base entity class for entities that track creation timestamp.
/// Inherits identity semantics from <see cref="BaseEntity"/> and adds creation tracking.
/// </summary>
public abstract class CreatedEntity : BaseEntity
{
    /// <summary>
    /// Gets the UTC timestamp when this entity was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CreatedEntity"/> class.
    /// Sets the creation timestamp to the current UTC time.
    /// Used when creating new entities.
    /// </summary>
    /// <remarks>
    /// NOTE: Uses DateTimeOffset.UtcNow which captures system time. This makes entities
    /// non-deterministic and harder to test. Consider introducing an ITimeProvider abstraction
    /// in a future refactoring for better testability.
    /// </remarks>
    protected CreatedEntity()
        : base()
    {
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CreatedEntity"/> class with a specified ID and creation timestamp.
    /// Used for entity rehydration from persistence.
    /// </summary>
    /// <param name="id">The unique identifier for this entity.</param>
    /// <param name="createdAt">The UTC timestamp when this entity was created.</param>
    protected CreatedEntity(Guid id, DateTimeOffset createdAt)
        : base(id)
    {
        CreatedAt = createdAt;
    }
}
