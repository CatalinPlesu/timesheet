using TimeSheet.Core.Domain.SharedKernel;
using TimeSheet.Core.Domain.Enums;

namespace TimeSheet.Core.Domain.Entities;

public class WorkDay : Entity<WorkDayId>
{
  private List<StateTransition> transitions { get; set; } = [];
  public IReadOnlyCollection<StateTransition> Transitions => transitions.AsReadOnly();

  public DateOnly Date { get; private set; }
  public UserId UserId { get; private set; }

  public WorkDayState CurrentState =>
    transitions.Count > 0 ? transitions.Last().State : WorkDayState.NotStarted;


  public static WorkDay StartToday(UserId userId)
  {
    return WorkDay.Create(userId, DateOnly.FromDateTime(DateTime.UtcNow));
  }
  public static WorkDay Create(UserId userId, DateOnly date)
  {
    return new WorkDay
    {
      UserId = userId,
      Date = date
    };
  }

  public void RecordTransition(WorkDayState toState, TimeOnly time)
  {
    var utcNow = TimeOnly.FromDateTime(DateTime.UtcNow);

    if (transitions.Count > 0)
    {
      var lastTransition = transitions.Last();
      if (utcNow <= lastTransition.Timestamp)
      {
        throw new InvalidOperationException("Transition timestamp must be after the last transition");
      }
    }

    transitions.Add(new StateTransition(toState, utcNow));
    MarkAsUpdated();
  }
}

public record StateTransition(WorkDayState State, TimeOnly Timestamp);
