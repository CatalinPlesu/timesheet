namespace TimeSheet.Core.Domain.ValueObjects;

public sealed record TimezoneSettings
{
  public int UtcOffsetHours { get; init; }

  private TimezoneSettings() { }

  public static TimezoneSettings Create(int utcOffsetHours)
  {
    if (utcOffsetHours < -12 || utcOffsetHours > 14)
      throw new ArgumentException("UTC offset must be between -12 and +14", nameof(utcOffsetHours));

    return new TimezoneSettings { UtcOffsetHours = utcOffsetHours };
  }

  public DateTimeOffset ToLocalTime(DateTime utcTime) =>
      new DateTimeOffset(utcTime, TimeSpan.Zero)
          .ToOffset(TimeSpan.FromHours(UtcOffsetHours));

  public DateTime ToUtcTime(DateTimeOffset localTime) =>
      localTime.UtcDateTime;
}
