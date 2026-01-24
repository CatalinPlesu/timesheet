namespace TimeSheet.Core.Domain.ValueObjects;

public sealed record UserPreferences
{
  public TimezoneSettings Timezone { get; init; }
  public WorkSchedule WorkSchedule { get; init; }
  public NotificationSettings Notifications { get; init; }

  private UserPreferences() { }

  public static UserPreferences Create(
      TimezoneSettings timezone,
      WorkSchedule workSchedule,
      NotificationSettings notifications)
  {
    return new UserPreferences
    {
      Timezone = timezone ?? throw new ArgumentNullException(nameof(timezone)),
      WorkSchedule = workSchedule ?? throw new ArgumentNullException(nameof(workSchedule)),
      Notifications = notifications ?? throw new ArgumentNullException(nameof(notifications))
    };
  }

  public static UserPreferences CreateDefault(int utcOffsetHours = 0)
  {
    return new UserPreferences
    {
      Timezone = TimezoneSettings.Create(utcOffsetHours),
      WorkSchedule = WorkSchedule.CreateStandard(),
      Notifications = NotificationSettings.CreateDefault()
    };
  }
}
