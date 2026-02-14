namespace TimeSheet.Presentation.API.Models.Analytics;

/// <summary>
/// DTO representing commute pattern analysis for a specific day of the week.
/// </summary>
public sealed class CommutePatternsDto
{
    /// <summary>
    /// Gets or sets the day of the week (0 = Sunday, 6 = Saturday).
    /// </summary>
    public required DayOfWeek DayOfWeek { get; set; }

    /// <summary>
    /// Gets or sets the average commute duration in hours for this day.
    /// </summary>
    public required decimal AverageDurationHours { get; set; }

    /// <summary>
    /// Gets or sets the optimal start time (hour) for shortest commute, or null if no data.
    /// </summary>
    public int? OptimalStartHour { get; set; }

    /// <summary>
    /// Gets or sets the shortest commute duration observed at the optimal time, in hours.
    /// </summary>
    public decimal? ShortestDurationHours { get; set; }

    /// <summary>
    /// Gets or sets the number of commute sessions analyzed for this day.
    /// </summary>
    public required int SessionCount { get; set; }
}
