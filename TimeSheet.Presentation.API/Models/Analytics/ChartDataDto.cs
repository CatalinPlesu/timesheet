namespace TimeSheet.Presentation.API.Models.Analytics;

/// <summary>
/// DTO representing chart data with idle time visualization.
/// </summary>
public sealed class ChartDataDto
{
    /// <summary>
    /// Gets or sets the labels for the chart (e.g., dates).
    /// </summary>
    public required List<string> Labels { get; set; }

    /// <summary>
    /// Gets or sets the work hours data points.
    /// </summary>
    public required List<decimal> WorkHours { get; set; }

    /// <summary>
    /// Gets or sets the commute hours data points.
    /// </summary>
    public required List<decimal> CommuteHours { get; set; }

    /// <summary>
    /// Gets or sets the lunch hours data points.
    /// </summary>
    public required List<decimal> LunchHours { get; set; }

    /// <summary>
    /// Gets or sets the idle time data points (time between first and last activity minus tracked time).
    /// </summary>
    public required List<decimal> IdleHours { get; set; }

    /// <summary>
    /// Gets or sets the total duration data points (first to last activity).
    /// </summary>
    public required List<decimal> TotalDurationHours { get; set; }
}
