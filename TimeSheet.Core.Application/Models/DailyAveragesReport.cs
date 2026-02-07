namespace TimeSheet.Core.Application.Models;

/// <summary>
/// Represents average daily statistics for a user over a specific time period.
/// </summary>
public record DailyAveragesReport
{
    /// <summary>
    /// Gets the number of days included in this report.
    /// </summary>
    public int DaysIncluded { get; init; }

    /// <summary>
    /// Gets the average work hours per day.
    /// </summary>
    public decimal AverageWorkHours { get; init; }

    /// <summary>
    /// Gets the average commute-to-work time in hours per day.
    /// </summary>
    public decimal AverageCommuteToWorkHours { get; init; }

    /// <summary>
    /// Gets the average commute-to-home time in hours per day.
    /// </summary>
    public decimal AverageCommuteToHomeHours { get; init; }

    /// <summary>
    /// Gets the average lunch duration in hours per day.
    /// </summary>
    public decimal AverageLunchHours { get; init; }

    /// <summary>
    /// Gets the total number of work days in the period (days with at least one work session).
    /// </summary>
    public int TotalWorkDays { get; init; }
}
