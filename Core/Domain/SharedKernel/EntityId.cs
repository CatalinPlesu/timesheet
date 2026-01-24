namespace TimeSheet.Core.Domain.SharedKernel;

// Interface with static abstract members
public interface IEntityId<T, TSelf>
    where TSelf : IEntityId<T, TSelf>
{
  static abstract TSelf New();
  static abstract TSelf From(T value);
  T GetValue();
}

// Generic base for typed IDs
public abstract record EntityId<T, TSelf>(T Value)
    where TSelf : EntityId<T, TSelf>
{
  public override string ToString() => Value?.ToString() ?? string.Empty;

  public virtual T GetValue() => Value;
}

public record UserId : EntityId<Guid, UserId>, IEntityId<Guid, UserId>
{
  public UserId(Guid value) : base(value) { }

  public static UserId New() => new(Guid.NewGuid());
  public static UserId From(Guid value) => new(value);
}

public record WorkDayId : EntityId<Guid, WorkDayId>, IEntityId<Guid, WorkDayId>
{
  public WorkDayId(Guid value) : base(value) { }

  public static WorkDayId New() => new(Guid.NewGuid());
  public static WorkDayId From(Guid value) => new(value);
}
