namespace TimeSheet.Core.Domain.ValueObjects;

public record NotificationPreferences
{
  public bool LunchReminderEnabled { get; init; } = true;
  public TimeOnly LunchReminderTime { get; init; } = new(12, 0);
  
  public bool EndOfDayReminderEnabled { get; init; } = true;
  public TimeOnly EndOfDayReminderTime { get; init; } = new(17, 0);
  
  public bool ClockOutReminderEnabled { get; init; } = true;
  public bool GoalAchievedNotificationEnabled { get; init; } = true;
}
