namespace TimeSheet.Presentation.API.Models.Analytics;

/// <summary>
/// DTO representing one day's data in a daily breakdown report.
/// </summary>
public sealed class DailyBreakdownDto
{
    /// <summary>
    /// Gets or sets the date for this row (UTC).
    /// </summary>
    public required DateTime Date { get; set; }

    /// <summary>
    /// Gets or sets the total work hours for this day.
    /// </summary>
    public required decimal WorkHours { get; set; }

    /// <summary>
    /// Gets or sets the commute to work time in hours for this day.
    /// </summary>
    public required decimal CommuteToWorkHours { get; set; }

    /// <summary>
    /// Gets or sets the commute to home time in hours for this day.
    /// </summary>
    public required decimal CommuteToHomeHours { get; set; }

    /// <summary>
    /// Gets or sets the lunch time in hours for this day.
    /// </summary>
    public required decimal LunchHours { get; set; }

    /// <summary>
    /// Gets or sets the total duration from first to last activity in hours for this day.
    /// </summary>
    public decimal? TotalDurationHours { get; set; }

    /// <summary>
    /// Gets or sets whether this day has any work activity.
    /// </summary>
    public required bool HasActivity { get; set; }
}
