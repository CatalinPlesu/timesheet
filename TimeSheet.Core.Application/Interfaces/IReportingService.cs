using TimeSheet.Core.Application.Models;
using TimeSheet.Core.Domain.Enums;

namespace TimeSheet.Core.Application.Interfaces;

/// <summary>
/// Application service for generating reports and analytics.
/// </summary>
public interface IReportingService
{
    /// <summary>
    /// Generates a daily averages report for a user over the last N days.
    /// </summary>
    /// <param name="userId">The Telegram user ID.</param>
    /// <param name="days">The number of days to include in the report (e.g., 7, 30, 90).</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A daily averages report containing average work, commute, and lunch times.</returns>
    Task<DailyAveragesReport> GetDailyAveragesAsync(
        long userId,
        int days,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates commute pattern analysis for a user, grouped by day of week.
    /// </summary>
    /// <param name="userId">The Telegram user ID.</param>
    /// <param name="direction">The commute direction to analyze.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A list of commute patterns, one for each day of the week that has data.</returns>
    Task<List<CommutePattern>> GetCommutePatternsAsync(
        long userId,
        CommuteDirection direction,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates an aggregate report for a specific time period.
    /// </summary>
    /// <param name="userId">The Telegram user ID.</param>
    /// <param name="startDate">The start date of the period (UTC).</param>
    /// <param name="endDate">The end date of the period (UTC).</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>An aggregate report with totals for the period.</returns>
    Task<PeriodAggregate> GetPeriodAggregateAsync(
        long userId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a daily breakdown report for a specific time period.
    /// Returns one row per day with detailed activity breakdown.
    /// </summary>
    /// <param name="userId">The Telegram user ID.</param>
    /// <param name="startDate">The start date of the period (UTC).</param>
    /// <param name="endDate">The end date of the period (UTC).</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A list of daily breakdown rows, one for each day in the period.</returns>
    Task<List<DailyBreakdownRow>> GetDailyBreakdownAsync(
        long userId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);
}
