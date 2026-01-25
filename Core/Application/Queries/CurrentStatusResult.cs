using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.Enums;

namespace TimeSheet.Core.Application.Queries;

public record CurrentStatusResult
{
  public WorkDayState CurrentState { get; init; }
  public IReadOnlyCollection<StateTransition> Transitions { get; init; } = Array.Empty<StateTransition>();

  public CurrentStatusResult(WorkDayState currentState, IReadOnlyCollection<StateTransition> transitions)
  {
    CurrentState = currentState;
    Transitions = transitions;
  }
}
