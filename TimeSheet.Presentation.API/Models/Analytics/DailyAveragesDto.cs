namespace TimeSheet.Presentation.API.Models.Analytics;

/// <summary>
/// DTO representing daily average statistics.
/// </summary>
public sealed class DailyAveragesDto
{
    /// <summary>
    /// Gets or sets the number of days included in this report.
    /// </summary>
    public required int DaysIncluded { get; set; }

    /// <summary>
    /// Gets or sets the average work hours per day.
    /// </summary>
    public required decimal AverageWorkHours { get; set; }

    /// <summary>
    /// Gets or sets the average commute-to-work time in hours per day.
    /// </summary>
    public required decimal AverageCommuteToWorkHours { get; set; }

    /// <summary>
    /// Gets or sets the average commute-to-home time in hours per day.
    /// </summary>
    public required decimal AverageCommuteToHomeHours { get; set; }

    /// <summary>
    /// Gets or sets the average lunch duration in hours per day.
    /// </summary>
    public required decimal AverageLunchHours { get; set; }

    /// <summary>
    /// Gets or sets the total number of work days in the period.
    /// </summary>
    public required int TotalWorkDays { get; set; }

    /// <summary>
    /// Gets or sets the average total duration from first to last activity per day in hours.
    /// </summary>
    public required decimal AverageTotalDurationHours { get; set; }
}
