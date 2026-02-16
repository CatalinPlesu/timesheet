namespace TimeSheet.Core.Application.Models;

/// <summary>
/// Represents aggregated statistics for a specific time period.
/// </summary>
public record PeriodAggregate
{
    /// <summary>
    /// Gets the start date of the period (UTC).
    /// </summary>
    public DateTime StartDate { get; init; }

    /// <summary>
    /// Gets the end date of the period (UTC).
    /// </summary>
    public DateTime EndDate { get; init; }

    /// <summary>
    /// Gets the total work hours in the period.
    /// </summary>
    public decimal TotalWorkHours { get; init; }

    /// <summary>
    /// Gets the total commute time in hours in the period.
    /// </summary>
    public decimal TotalCommuteHours { get; init; }

    /// <summary>
    /// Gets the total lunch time in hours in the period.
    /// </summary>
    public decimal TotalLunchHours { get; init; }

    /// <summary>
    /// Gets the number of work days in the period.
    /// </summary>
    public int WorkDaysCount { get; init; }

    /// <summary>
    /// Gets the average daily duration from first to last activity in hours.
    /// This represents the average "at work" span per day, including idle time between sessions.
    /// For example: if Monday is 9h (8am-5pm) and Tuesday is 10h (8am-6pm), this will be 9.5h.
    /// Null if there are no sessions in the period.
    /// </summary>
    public decimal? TotalDurationHours { get; init; }
}
