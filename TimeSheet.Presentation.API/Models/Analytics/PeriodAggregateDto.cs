namespace TimeSheet.Presentation.API.Models.Analytics;

/// <summary>
/// DTO representing aggregated statistics for a specific time period.
/// </summary>
public sealed class PeriodAggregateDto
{
    /// <summary>
    /// Gets or sets the start date of the period (UTC).
    /// </summary>
    public required DateTime StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date of the period (UTC).
    /// </summary>
    public required DateTime EndDate { get; set; }

    /// <summary>
    /// Gets or sets the total work hours in the period.
    /// </summary>
    public required decimal TotalWorkHours { get; set; }

    /// <summary>
    /// Gets or sets the total commute-to-work time in hours in the period.
    /// </summary>
    public required decimal TotalCommuteToWorkHours { get; set; }

    /// <summary>
    /// Gets or sets the total commute-to-home time in hours in the period.
    /// </summary>
    public required decimal TotalCommuteToHomeHours { get; set; }

    /// <summary>
    /// Gets or sets the total commute time (to-work + to-home) in hours in the period.
    /// </summary>
    public decimal TotalCommuteHours => TotalCommuteToWorkHours + TotalCommuteToHomeHours;

    /// <summary>
    /// Gets or sets the total lunch time in hours in the period.
    /// </summary>
    public required decimal TotalLunchHours { get; set; }

    /// <summary>
    /// Gets or sets the number of work days in the period.
    /// </summary>
    public required int WorkDaysCount { get; set; }

    /// <summary>
    /// Gets or sets the average daily duration from first to last activity in hours.
    /// This represents the average "at work" span per day, including idle time between sessions.
    /// </summary>
    public decimal? TotalDurationHours { get; set; }
}
