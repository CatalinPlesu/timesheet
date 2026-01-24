namespace TimeSheet.Core.Domain.ValueObjects;

public sealed record WorkSchedule
{
  public TimeSpan ExpectedDailyWorkHours { get; init; }
  public TimeOnly? TypicalWorkStartTime { get; init; }
  public TimeOnly? TypicalLunchHour { get; init; }
  public DayOfWeek WeekStart { get; init; }
  public int DaysWorkingPerWeek { get; init; }

  // Simple holiday list
  public ISet<DateOnly> Holidays { get; init; }

  private WorkSchedule() { }

  public static WorkSchedule Create(
      TimeSpan expectedDailyWorkHours,
      DayOfWeek weekStart,
      int daysWorkingPerWeek,
      TimeOnly? typicalWorkStartTime = null,
      TimeOnly? typicalLunchHour = null,
      IEnumerable<DateOnly>? holidays = null)
  {
    if (expectedDailyWorkHours <= TimeSpan.Zero || expectedDailyWorkHours > TimeSpan.FromHours(24))
      throw new ArgumentException("Work hours must be between 0 and 24", nameof(expectedDailyWorkHours));

    if (daysWorkingPerWeek < 0 || daysWorkingPerWeek > 7)
      throw new ArgumentException("Days working per week must be between 0 and 7", nameof(daysWorkingPerWeek));

    return new WorkSchedule
    {
      ExpectedDailyWorkHours = expectedDailyWorkHours,
      TypicalWorkStartTime = typicalWorkStartTime,
      TypicalLunchHour = typicalLunchHour,
      WeekStart = weekStart,
      DaysWorkingPerWeek = daysWorkingPerWeek,
      Holidays = holidays?.ToHashSet() ?? new HashSet<DateOnly>()
    };
  }

  // Check if a specific date is a work day based on week configuration and holidays
  public bool IsScheduledWorkDay(DateOnly date) =>
      !Holidays.Contains(date) && IsWorkingDayOfWeek(date.DayOfWeek);

  // Determine if the given day of week is a working day
  private bool IsWorkingDayOfWeek(DayOfWeek dayOfWeek)
  {
    var daysFromWeekStart = ((int)dayOfWeek - (int)WeekStart + 7) % 7;
    return daysFromWeekStart < DaysWorkingPerWeek;
  }

  // Add a holiday
  public WorkSchedule AddHoliday(DateOnly holiday) =>
      this with { Holidays = Holidays.Append(holiday).ToHashSet() };

  // Add multiple holidays
  public WorkSchedule AddHolidays(IEnumerable<DateOnly> holidays) =>
      this with { Holidays = Holidays.Union(holidays).ToHashSet() };

  // Remove a holiday
  public WorkSchedule RemoveHoliday(DateOnly holiday) =>
      this with { Holidays = Holidays.Where(h => h != holiday).ToHashSet() };

  // Clear all holidays
  public WorkSchedule ClearHolidays() =>
      this with { Holidays = new HashSet<DateOnly>() };

  // Expected weekly hours
  public double ExpectedWeeklyHours() =>
      DaysWorkingPerWeek * ExpectedDailyWorkHours.TotalHours;
}
