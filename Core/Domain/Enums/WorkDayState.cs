namespace TimeSheet.Core.Domain.Enums;

public enum WorkDayState
{
  NotStarted,
  CommutingToWork,
  AtWork,
  Working,
  OnLunch,
  CommutingHome,
  AtHome,

  // Special day states
  SickDay,
  Vacation,
  Holiday
}
