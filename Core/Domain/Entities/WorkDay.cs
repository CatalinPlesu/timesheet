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

    // Prevent redundant transitions to the same state (except Working which allows multiple entries)
    if (fromState == toState && toState != WorkDayState.Working)
    {
      throw new InvalidOperationException($"Already in state {toState}");
    }

    // Validate chronological order
    if (_transitions.Count > 0)
    {
      var lastTransition = _transitions.Last();
      if (time <= lastTransition.Timestamp)
      {
        throw new InvalidOperationException("Transition timestamp must be after the last transition");
      }
    }

    // Get the path of implicit transitions needed
    var transitionPath = GetTransitionPath(fromState, toState);
    
    if (transitionPath.Count == 0)
    {
      throw new InvalidOperationException($"Invalid state transition from {fromState} to {toState}");
    }

    // Record all transitions in the path with the same timestamp
    foreach (var state in transitionPath)
    {
      _transitions.Add(new StateTransition(fromState, state, time));
      fromState = state;
    }
    
    MarkAsUpdated();
  }

  /// <summary>
  /// Gets the path of transitions needed to go from one state to another.
  /// Returns empty list if transition is invalid.
  /// Supports implicit transitions (e.g., CommutingToWork -> Working implies AtWork in between).
  /// </summary>
  private static List<WorkDayState> GetTransitionPath(WorkDayState from, WorkDayState to)
  {
    // Emergency exit: any state can transition directly to AtHome
    if (to == WorkDayState.AtHome && from != WorkDayState.AtHome)
    {
      return new List<WorkDayState> { WorkDayState.AtHome };
    }

    // Special days: any state can transition to special days
    if (to is WorkDayState.SickDay or WorkDayState.Vacation or WorkDayState.Holiday)
    {
      return new List<WorkDayState> { to };
    }

    // Special days can only transition to AtHome
    if (from is WorkDayState.SickDay or WorkDayState.Vacation or WorkDayState.Holiday)
    {
      if (to == WorkDayState.AtHome)
      {
        return new List<WorkDayState> { WorkDayState.AtHome };
      }
      return new List<WorkDayState>();
    }

    // Direct transitions (no implicit states needed)
    var directTransitions = new Dictionary<(WorkDayState, WorkDayState), List<WorkDayState>>
    {
      // Standard progression
      [(WorkDayState.NotStarted, WorkDayState.CommutingToWork)] = new() { WorkDayState.CommutingToWork },
      [(WorkDayState.CommutingToWork, WorkDayState.AtWork)] = new() { WorkDayState.AtWork },
      [(WorkDayState.AtWork, WorkDayState.Working)] = new() { WorkDayState.Working },
      [(WorkDayState.Working, WorkDayState.OnLunch)] = new() { WorkDayState.OnLunch },
      [(WorkDayState.OnLunch, WorkDayState.Working)] = new() { WorkDayState.Working },
      [(WorkDayState.Working, WorkDayState.CommutingHome)] = new() { WorkDayState.CommutingHome },
      [(WorkDayState.CommutingHome, WorkDayState.AtHome)] = new() { WorkDayState.AtHome },

      // Remote work: skip commute states
      [(WorkDayState.NotStarted, WorkDayState.Working)] = new() { WorkDayState.Working },

      // Multiple lunch breaks (same state is allowed for Working)
      [(WorkDayState.Working, WorkDayState.Working)] = new() { WorkDayState.Working },
    };

    // Check for direct transition
    if (directTransitions.TryGetValue((from, to), out var directPath))
    {
      return directPath;
    }

    // Implicit transitions - fill in the gaps
    return (from, to) switch
    {
      // From commuting to work: implies arriving at work first
      (WorkDayState.CommutingToWork, WorkDayState.Working) => new() { WorkDayState.AtWork, WorkDayState.Working },
      (WorkDayState.CommutingToWork, WorkDayState.OnLunch) => new() { WorkDayState.AtWork, WorkDayState.Working, WorkDayState.OnLunch },
      (WorkDayState.CommutingToWork, WorkDayState.CommutingHome) => new() { WorkDayState.AtWork, WorkDayState.Working, WorkDayState.CommutingHome },

      // From at work: skip to lunch or commute home
      (WorkDayState.AtWork, WorkDayState.OnLunch) => new() { WorkDayState.Working, WorkDayState.OnLunch },
      (WorkDayState.AtWork, WorkDayState.CommutingHome) => new() { WorkDayState.Working, WorkDayState.CommutingHome },

      // From working: skip to commute home (implies finishing work)
      
      // From lunch: skip to commute home
      (WorkDayState.OnLunch, WorkDayState.CommutingHome) => new() { WorkDayState.Working, WorkDayState.CommutingHome },

      // From not started: skip to lunch or home (remote work patterns)
      (WorkDayState.NotStarted, WorkDayState.OnLunch) => new() { WorkDayState.Working, WorkDayState.OnLunch },
      (WorkDayState.NotStarted, WorkDayState.CommutingHome) => new() { WorkDayState.Working, WorkDayState.CommutingHome },

      _ => new List<WorkDayState>()
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
