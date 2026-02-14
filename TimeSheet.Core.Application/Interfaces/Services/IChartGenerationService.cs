using TimeSheet.Core.Application.Models;

namespace TimeSheet.Core.Application.Interfaces.Services;

/// <summary>
/// Service for generating chart images from report data.
/// </summary>
public interface IChartGenerationService
{
    /// <summary>
    /// Generates a bar chart showing work hours by day from a daily breakdown report.
    /// </summary>
    /// <param name="breakdown">The daily breakdown data.</param>
    /// <param name="periodLabel">Label for the period (e.g., "This Week", "January 2026").</param>
    /// <returns>PNG image data as a byte array.</returns>
    byte[] GenerateDailyBreakdownChart(List<DailyBreakdownRow> breakdown, string periodLabel);

    /// <summary>
    /// Generates a line chart showing work hours trend over time.
    /// </summary>
    /// <param name="breakdown">The daily breakdown data.</param>
    /// <param name="periodLabel">Label for the period.</param>
    /// <returns>PNG image data as a byte array.</returns>
    byte[] GenerateWorkHoursTrendChart(List<DailyBreakdownRow> breakdown, string periodLabel);

    /// <summary>
    /// Generates a stacked bar chart showing the breakdown of activities (work, commute, lunch) by day.
    /// </summary>
    /// <param name="breakdown">The daily breakdown data.</param>
    /// <param name="periodLabel">Label for the period.</param>
    /// <returns>PNG image data as a byte array.</returns>
    byte[] GenerateActivityBreakdownChart(List<DailyBreakdownRow> breakdown, string periodLabel);

    /// <summary>
    /// Generates a horizontal bar chart comparing daily averages across multiple time periods.
    /// </summary>
    /// <param name="report7Days">7-day average report.</param>
    /// <param name="report30Days">30-day average report.</param>
    /// <param name="report90Days">90-day average report.</param>
    /// <returns>PNG image data as a byte array.</returns>
    byte[] GenerateDailyAveragesComparisonChart(
        DailyAveragesReport report7Days,
        DailyAveragesReport report30Days,
        DailyAveragesReport report90Days);

    /// <summary>
    /// Generates a grouped bar chart showing commute patterns by day of week.
    /// </summary>
    /// <param name="toWorkPatterns">Commute-to-work patterns.</param>
    /// <param name="toHomePatterns">Commute-to-home patterns.</param>
    /// <returns>PNG image data as a byte array.</returns>
    byte[] GenerateCommutePatternsChart(
        List<CommutePattern> toWorkPatterns,
        List<CommutePattern> toHomePatterns);
}
