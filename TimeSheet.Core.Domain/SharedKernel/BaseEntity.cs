namespace TimeSheet.Core.Domain.SharedKernel;

/// <summary>
/// Base entity class providing identity and equality semantics.
/// All domain entities inherit from this class directly or indirectly.
/// </summary>
public abstract class BaseEntity : IEquatable<BaseEntity>
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

    /// <summary>
    /// Determines whether the specified entity is equal to the current entity.
    /// Entities are considered equal if they have the same ID.
    /// </summary>
    /// <param name="other">The entity to compare with the current entity.</param>
    /// <returns>true if the specified entity is equal to the current entity; otherwise, false.</returns>
    public bool Equals(BaseEntity? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (GetType() != other.GetType())
            return false;

        return Id == other.Id;
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current entity.
    /// </summary>
    /// <param name="obj">The object to compare with the current entity.</param>
    /// <returns>true if the specified object is equal to the current entity; otherwise, false.</returns>
    public override bool Equals(object? obj)
    {
        return Equals(obj as BaseEntity);
    }

    /// <summary>
    /// Returns the hash code for this entity based on its ID.
    /// </summary>
    /// <returns>A hash code for the current entity.</returns>
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    /// <summary>
    /// Determines whether two entities are equal.
    /// </summary>
    /// <param name="left">The first entity to compare.</param>
    /// <param name="right">The second entity to compare.</param>
    /// <returns>true if the entities are equal; otherwise, false.</returns>
    public static bool operator ==(BaseEntity? left, BaseEntity? right)
    {
        return left?.Equals(right) ?? right is null;
    }

    /// <summary>
    /// Determines whether two entities are not equal.
    /// </summary>
    /// <param name="left">The first entity to compare.</param>
    /// <param name="right">The second entity to compare.</param>
    /// <returns>true if the entities are not equal; otherwise, false.</returns>
    public static bool operator !=(BaseEntity? left, BaseEntity? right)
    {
        return !(left == right);
    }
}
