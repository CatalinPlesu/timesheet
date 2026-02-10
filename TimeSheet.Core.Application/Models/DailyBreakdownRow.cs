namespace TimeSheet.Core.Application.Models;

/// <summary>
/// Represents one day's data in a daily breakdown report table.
/// </summary>
public record DailyBreakdownRow
{
    /// <summary>
    /// Gets the date for this row (UTC).
    /// </summary>
    public DateTime Date { get; init; }

    /// <summary>
    /// Gets the total work hours for this day.
    /// </summary>
    public decimal WorkHours { get; init; }

    /// <summary>
    /// Gets the commute to work time in hours for this day.
    /// </summary>
    public decimal CommuteToWorkHours { get; init; }

    /// <summary>
    /// Gets the commute to home time in hours for this day.
    /// </summary>
    public decimal CommuteToHomeHours { get; init; }

    /// <summary>
    /// Gets the lunch time in hours for this day.
    /// </summary>
    public decimal LunchHours { get; init; }

    /// <summary>
    /// Gets the total duration from first to last activity in hours for this day.
    /// Null if there are no sessions on this day.
    /// </summary>
    public decimal? TotalDurationHours { get; init; }

    /// <summary>
    /// Gets whether this day has any work activity.
    /// </summary>
    public bool HasActivity { get; init; }
}
