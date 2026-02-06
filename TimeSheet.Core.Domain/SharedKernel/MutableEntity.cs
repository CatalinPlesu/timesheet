namespace TimeSheet.Core.Domain.SharedKernel;

/// <summary>
/// Base entity class for entities that track both creation and modification timestamps.
/// Inherits identity and creation tracking from <see cref="CreatedEntity"/> and adds mutation tracking.
/// </summary>
public abstract class MutableEntity : CreatedEntity
{
    /// <summary>
    /// Gets the UTC timestamp when this entity was last modified.
    /// Returns null if the entity has never been modified since creation.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MutableEntity"/> class.
    /// Sets the creation timestamp to the current UTC time and UpdatedAt to null.
    /// Used when creating new entities.
    /// </summary>
    protected MutableEntity()
        : base()
    {
        UpdatedAt = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MutableEntity"/> class with a specified ID, creation timestamp, and optional update timestamp.
    /// Used for entity rehydration from persistence.
    /// </summary>
    /// <param name="id">The unique identifier for this entity.</param>
    /// <param name="createdAt">The UTC timestamp when this entity was created.</param>
    /// <param name="updatedAt">The UTC timestamp when this entity was last modified, or null if never modified.</param>
    protected MutableEntity(Guid id, DateTimeOffset createdAt, DateTimeOffset? updatedAt)
        : base(id, createdAt)
    {
        UpdatedAt = updatedAt;
    }

    /// <summary>
    /// Marks this entity as modified by setting the UpdatedAt timestamp to the current UTC time.
    /// This method should be called by derived entities whenever their state changes.
    /// </summary>
    /// <remarks>
    /// NOTE: Uses DateTimeOffset.UtcNow which captures system time. This makes the mutation
    /// non-deterministic and harder to test. Consider introducing an ITimeProvider abstraction
    /// in a future refactoring for better testability.
    /// </remarks>
    protected void MarkAsModified()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
