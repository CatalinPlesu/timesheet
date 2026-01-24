using TimeSheet.Core.Domain.SharedKernel;
using TimeSheet.Core.Domain.Enums;

namespace TimeSheet.Core.Domain.Entities;

public class WorkDay : Entity<WorkDayId>
{
  // EF Core needs to be able to set this
  public List<StateTransition> _transitions { get; set; } = new();
  public IReadOnlyCollection<StateTransition> Transitions => _transitions.AsReadOnly();

  public DateOnly Date { get; private set; }
  public UserId UserId { get; private set; } = null!;

  public WorkDayState CurrentState =>
    _transitions.Count > 0 ? _transitions.Last().ToState : WorkDayState.NotStarted;

  // Private constructor for EF Core
  private WorkDay()
  {
    // EF Core will populate these properties
  }

  public static WorkDay StartToday(UserId userId)
  {
    return WorkDay.Create(userId, DateOnly.FromDateTime(DateTime.UtcNow));
  }

  public static WorkDay Create(UserId userId, DateOnly date)
  {
    return new WorkDay
    {
      UserId = userId,
      Date = date,
      _transitions = new List<StateTransition>()
    };
  }

  public void RecordTransition(WorkDayState toState, TimeOnly time)
  {
    var fromState = CurrentState;

    // Validate chronological order
    if (_transitions.Count > 0)
    {
      var lastTransition = _transitions.Last();
      if (time <= lastTransition.Timestamp)
      {
        throw new InvalidOperationException("Transition timestamp must be after the last transition");
      }
    }

    // Validate state transition
    if (!IsValidTransition(fromState, toState))
    {
      throw new InvalidOperationException($"Invalid state transition from {fromState} to {toState}");
    }

    _transitions.Add(new StateTransition(fromState, toState, time));
    MarkAsUpdated();
  }

  private static bool IsValidTransition(WorkDayState from, WorkDayState to)
  {
    // Emergency exit: any state can transition to AtHome
    if (to == WorkDayState.AtHome)
    {
      return true;
    }

    // Special days can only transition to AtHome
    if (from is WorkDayState.SickDay or WorkDayState.Vacation or WorkDayState.Holiday)
    {
      return to == WorkDayState.AtHome;
    }

    // Any state can transition to special days
    if (to is WorkDayState.SickDay or WorkDayState.Vacation or WorkDayState.Holiday)
    {
      return true;
    }

    // Regular state transitions
    return (from, to) switch
    {
      // Standard progression
      (WorkDayState.NotStarted, WorkDayState.CommutingToWork) => true,
      (WorkDayState.CommutingToWork, WorkDayState.AtWork) => true,
      (WorkDayState.AtWork, WorkDayState.Working) => true,
      (WorkDayState.Working, WorkDayState.OnLunch) => true,
      (WorkDayState.OnLunch, WorkDayState.Working) => true,
      (WorkDayState.Working, WorkDayState.CommutingHome) => true,
      (WorkDayState.CommutingHome, WorkDayState.AtHome) => true,

      // Remote work: skip commute states
      (WorkDayState.NotStarted, WorkDayState.Working) => true,

      // Multiple lunch breaks
      (WorkDayState.Working, WorkDayState.Working) => true,

      _ => false
    };
  }
}

public record StateTransition
{
  public WorkDayState FromState { get; init; }
  public WorkDayState ToState { get; init; }
  public TimeOnly Timestamp { get; init; }

  public StateTransition(WorkDayState fromState, WorkDayState toState, TimeOnly timestamp)
  {
    FromState = fromState;
    ToState = toState;
    Timestamp = timestamp;
  }

  // Parameterless constructor for EF Core
  private StateTransition() { }
}
