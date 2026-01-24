namespace TimeSheet.Core.Domain.SharedKernel;

public abstract class Entity<TId> where TId : EntityId<Guid, TId>, IEntityId<Guid, TId>
{
  public TId Id { get; protected set; }
  public DateTime CreatedAt { get; private set; }
  public DateTime UpdatedAt { get; private set; }

  protected Entity()
  {
    Id = TId.New();
    CreatedAt = DateTime.UtcNow;
    UpdatedAt = DateTime.UtcNow;
  }

  protected Entity(TId id)
  {
    Id = id;
    CreatedAt = DateTime.UtcNow;
    UpdatedAt = DateTime.UtcNow;
  }

  protected Entity(Guid id)
  {
    Id = TId.From(id);
    CreatedAt = DateTime.UtcNow;
    UpdatedAt = DateTime.UtcNow;
  }

  public void MarkAsUpdated()
  {
    UpdatedAt = DateTime.UtcNow;
  }

}
