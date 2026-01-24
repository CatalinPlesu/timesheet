using TimeSheet.Core.Domain.Enums;
using TimeSheet.Core.Domain.SharedKernel;

namespace TimeSheet.Core.Application.Commands;

public record RecordTransitionCommand
{
  public UserId UserId { get; init; }
  public DateOnly Date { get; init; }
  public WorkDayState ToState { get; init; }
  public TimeOnly Timestamp { get; init; }

  public RecordTransitionCommand(UserId userId, DateOnly date, WorkDayState toState, TimeOnly timestamp)
  {
    UserId = userId;
    Date = date;
    ToState = toState;
    Timestamp = timestamp;
  }
}
