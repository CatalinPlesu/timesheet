namespace TimeSheet.Presentation.API.Models.Analytics;

/// <summary>
/// Statistical metrics for a single tracked activity type.
/// </summary>
public sealed class ActivityStatsDto
{
    /// <summary>Average hours per day with data.</summary>
    public required double Avg { get; set; }

    /// <summary>Minimum hours on a single day.</summary>
    public required double Min { get; set; }

    /// <summary>Maximum hours on a single day.</summary>
    public required double Max { get; set; }

    /// <summary>Sample standard deviation (N-1) of daily hours.</summary>
    public required double StdDev { get; set; }

    /// <summary>Total hours over the whole period.</summary>
    public required double Total { get; set; }
}

/// <summary>
/// Summary statistics for all tracked activities over a given number of days.
/// </summary>
public sealed class StatsSummaryDto
{
    /// <summary>Number of calendar days covered by the query.</summary>
    public required int PeriodDays { get; set; }

    /// <summary>Number of days that have at least one completed session.</summary>
    public required int DaysWithData { get; set; }

    /// <summary>Work-session statistics.</summary>
    public required ActivityStatsDto Work { get; set; }

    /// <summary>Total commute statistics (to-work + to-home).</summary>
    public required ActivityStatsDto Commute { get; set; }

    /// <summary>Commute-to-work statistics.</summary>
    public required ActivityStatsDto CommuteToWork { get; set; }

    /// <summary>Commute-to-home statistics.</summary>
    public required ActivityStatsDto CommuteToHome { get; set; }

    /// <summary>Lunch-break statistics.</summary>
    public required ActivityStatsDto Lunch { get; set; }
}
