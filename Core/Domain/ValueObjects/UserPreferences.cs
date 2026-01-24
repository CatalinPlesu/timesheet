namespace TimeSheet.Core.Domain.ValueObjects;

public sealed record UserPreferences
{
  public int UtcOffsetHours { get; init; }
  public TimeSpan ExpectedDailyWorkHours { get; init; }
  public TimeOnly? TypicalWorkStartTime { get; init; }
  public TimeOnly? TypicalLunchHour { get; init; }
  public DayOfWeek WeekStart { get; init; }
  public int DaysWorkingPerWeek { get; init; }
  public ISet<DateOnly> Holidays { get; init; }
  public bool NotifyWhenWorkHoursComplete { get; init; }

  private UserPreferences() { }

  public static UserPreferences Create(
      int utcOffsetHours = 0,
      TimeSpan expectedDailyWorkHours = default,
      DayOfWeek weekStart = DayOfWeek.Monday,
      int daysWorkingPerWeek = 5,
      TimeOnly? typicalWorkStartTime = null,
      TimeOnly? typicalLunchHour = null,
      IEnumerable<DateOnly>? holidays = null,
      bool notifyWhenWorkHoursComplete = true)
  {
    if (utcOffsetHours < -12 || utcOffsetHours > 14)
      throw new ArgumentException("UTC offset must be between -12 and +14", nameof(utcOffsetHours));

    if (expectedDailyWorkHours <= TimeSpan.Zero || expectedDailyWorkHours > TimeSpan.FromHours(24))
      throw new ArgumentException("Work hours must be between 0 and 24", nameof(expectedDailyWorkHours));

    if (daysWorkingPerWeek < 0 || daysWorkingPerWeek > 7)
      throw new ArgumentException("Days working per week must be between 0 and 7", nameof(daysWorkingPerWeek));

    return new UserPreferences
    {
      UtcOffsetHours = utcOffsetHours,
      ExpectedDailyWorkHours = expectedDailyWorkHours,
      TypicalWorkStartTime = typicalWorkStartTime,
      TypicalLunchHour = typicalLunchHour,
      WeekStart = weekStart,
      DaysWorkingPerWeek = daysWorkingPerWeek,
      Holidays = holidays?.ToHashSet() ?? new HashSet<DateOnly>(),
      NotifyWhenWorkHoursComplete = notifyWhenWorkHoursComplete
    };
  }

  public static UserPreferences CreateDefault() =>
      Create(
          utcOffsetHours: 0,
          expectedDailyWorkHours: TimeSpan.FromHours(8),
          weekStart: DayOfWeek.Monday,
          daysWorkingPerWeek: 5,
          typicalWorkStartTime: new TimeOnly(9, 0),
          typicalLunchHour: new TimeOnly(13, 0),
          notifyWhenWorkHoursComplete: true);

  public bool IsScheduledWorkDay(DateOnly date) =>
      !Holidays.Contains(date) && IsWorkingDayOfWeek(date.DayOfWeek);

  private bool IsWorkingDayOfWeek(DayOfWeek dayOfWeek)
  {
    var daysFromWeekStart = ((int)dayOfWeek - (int)WeekStart + 7) % 7;
    return daysFromWeekStart < DaysWorkingPerWeek;
  }

  public double ExpectedWeeklyHours() =>
      DaysWorkingPerWeek * ExpectedDailyWorkHours.TotalHours;
}
