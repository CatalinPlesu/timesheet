namespace TimeSheet.Core.Domain.ValueObjects;

public sealed record NotificationSettings
{
  public bool NotifyWhenWorkHoursComplete { get; init; }

  private NotificationSettings() { }

  public static NotificationSettings Create(
      bool notifyWhenWorkHoursComplete = true)
  {
    return new NotificationSettings
    {
      NotifyWhenWorkHoursComplete = notifyWhenWorkHoursComplete,
    };
  }

  public static NotificationSettings CreateDefault() =>
      Create(
          notifyWhenWorkHoursComplete: true,
          lunchReminder: LunchBreakReminder.AfterHours(TimeSpan.FromHours(4))
      );

  public static NotificationSettings CreateMinimal() =>
      Create(notifyWhenWorkHoursComplete: false, lunchReminder: null);
}

