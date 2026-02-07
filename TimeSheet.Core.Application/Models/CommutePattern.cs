namespace TimeSheet.Core.Application.Models;

/// <summary>
/// Represents commute pattern analysis for a specific day of the week.
/// </summary>
public record CommutePattern
{
    /// <summary>
    /// Gets the day of the week (0 = Sunday, 6 = Saturday).
    /// </summary>
    public DayOfWeek DayOfWeek { get; init; }

    /// <summary>
    /// Gets the average commute duration in hours for this day.
    /// </summary>
    public decimal AverageDurationHours { get; init; }

    /// <summary>
    /// Gets the optimal start time (hour) for shortest commute, or null if no data.
    /// </summary>
    public int? OptimalStartHour { get; init; }

    /// <summary>
    /// Gets the shortest commute duration observed at the optimal time, in hours.
    /// Null if no data is available.
    /// </summary>
    public decimal? ShortestDurationHours { get; init; }

    /// <summary>
    /// Gets the number of commute sessions analyzed for this day.
    /// </summary>
    public int SessionCount { get; init; }
}
