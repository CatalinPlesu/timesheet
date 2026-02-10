using TimeSheet.Core.Application.Interfaces;
using TimeSheet.Core.Application.Models;
using TimeSheet.Core.Domain.Enums;

namespace TimeSheet.Core.Application.Services;

/// <summary>
/// Application service for generating reports and analytics.
/// </summary>
public class ReportingService(ITrackingSessionRepository trackingSessionRepository) : IReportingService
{
    /// <summary>
    /// Generates a daily averages report for a user over the last N days.
    /// </summary>
    /// <param name="userId">The Telegram user ID.</param>
    /// <param name="days">The number of days to include in the report (e.g., 7, 30, 90).</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A daily averages report containing average work, commute, and lunch times.</returns>
    public async Task<DailyAveragesReport> GetDailyAveragesAsync(
        long userId,
        int days,
        CancellationToken cancellationToken = default)
    {
        var endDate = DateTime.UtcNow.Date.AddDays(1);
        var startDate = endDate.AddDays(-days);

        var sessions = await trackingSessionRepository.GetSessionsInRangeAsync(
            userId,
            startDate,
            endDate,
            cancellationToken);

        // Group sessions by day
        var sessionsByDay = sessions
            .GroupBy(s => s.StartedAt.Date)
            .ToList();

        var daysIncluded = sessionsByDay.Count;

        if (daysIncluded == 0)
        {
            return new DailyAveragesReport
            {
                DaysIncluded = 0,
                AverageWorkHours = 0,
                AverageCommuteToWorkHours = 0,
                AverageCommuteToHomeHours = 0,
                AverageLunchHours = 0,
                TotalWorkDays = 0
            };
        }

        // Calculate totals
        decimal totalWorkHours = 0;
        decimal totalCommuteToWorkHours = 0;
        decimal totalCommuteToHomeHours = 0;
        decimal totalLunchHours = 0;
        decimal totalDurationHours = 0;
        int workDays = 0;

        foreach (var dayGroup in sessionsByDay)
        {
            var daySessions = dayGroup.ToList();
            bool hasWork = false;

            foreach (var session in daySessions)
            {
                if (session.EndedAt == null) continue;

                var duration = (decimal)(session.EndedAt.Value - session.StartedAt).TotalHours;

                switch (session.State)
                {
                    case TrackingState.Working:
                        totalWorkHours += duration;
                        hasWork = true;
                        break;

                    case TrackingState.Commuting:
                        if (session.CommuteDirection == CommuteDirection.ToWork)
                        {
                            totalCommuteToWorkHours += duration;
                        }
                        else
                        {
                            totalCommuteToHomeHours += duration;
                        }
                        break;

                    case TrackingState.Lunch:
                        totalLunchHours += duration;
                        break;
                }
            }

            // Calculate total duration for this day (first to last activity)
            if (daySessions.Any())
            {
                var firstActivityStart = daySessions.Min(s => s.StartedAt);
                var lastActivityEnd = daySessions.Max(s => s.EndedAt ?? DateTime.UtcNow);
                totalDurationHours += (decimal)(lastActivityEnd - firstActivityStart).TotalHours;
            }

            if (hasWork)
            {
                workDays++;
            }
        }

        return new DailyAveragesReport
        {
            DaysIncluded = daysIncluded,
            AverageWorkHours = workDays > 0 ? totalWorkHours / workDays : 0,
            AverageCommuteToWorkHours = workDays > 0 ? totalCommuteToWorkHours / workDays : 0,
            AverageCommuteToHomeHours = workDays > 0 ? totalCommuteToHomeHours / workDays : 0,
            AverageLunchHours = workDays > 0 ? totalLunchHours / workDays : 0,
            TotalWorkDays = workDays,
            AverageTotalDurationHours = workDays > 0 ? totalDurationHours / workDays : 0
        };
    }

    /// <summary>
    /// Generates commute pattern analysis for a user, grouped by day of week.
    /// </summary>
    /// <param name="userId">The Telegram user ID.</param>
    /// <param name="direction">The commute direction to analyze.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A list of commute patterns, one for each day of the week that has data.</returns>
    public async Task<List<CommutePattern>> GetCommutePatternsAsync(
        long userId,
        CommuteDirection direction,
        CancellationToken cancellationToken = default)
    {
        // Analyze last 90 days of data
        var endDate = DateTime.UtcNow.Date.AddDays(1);
        var startDate = endDate.AddDays(-90);

        var sessions = await trackingSessionRepository.GetSessionsInRangeAsync(
            userId,
            startDate,
            endDate,
            cancellationToken);

        var commuteSessions = sessions
            .Where(s => s.State == TrackingState.Commuting
                     && s.CommuteDirection == direction
                     && s.EndedAt != null)
            .Select(s => new
            {
                DayOfWeek = s.StartedAt.DayOfWeek,
                StartHour = s.StartedAt.Hour,
                DurationHours = (decimal)(s.EndedAt!.Value - s.StartedAt).TotalHours
            })
            .ToList();

        // Group by day of week
        var patterns = commuteSessions
            .GroupBy(s => s.DayOfWeek)
            .Select(g =>
            {
                var sessionsForDay = g.ToList();
                var averageDuration = sessionsForDay.Average(s => s.DurationHours);

                // Find optimal start hour (hour with shortest average commute)
                var byHour = sessionsForDay
                    .GroupBy(s => s.StartHour)
                    .Select(h => new
                    {
                        Hour = h.Key,
                        AverageDuration = h.Average(s => s.DurationHours),
                        Count = h.Count()
                    })
                    .Where(h => h.Count >= 2) // Only consider hours with at least 2 samples
                    .OrderBy(h => h.AverageDuration)
                    .FirstOrDefault();

                return new CommutePattern
                {
                    DayOfWeek = g.Key,
                    AverageDurationHours = averageDuration,
                    OptimalStartHour = byHour?.Hour,
                    ShortestDurationHours = byHour?.AverageDuration,
                    SessionCount = sessionsForDay.Count
                };
            })
            .OrderBy(p => p.DayOfWeek)
            .ToList();

        return patterns;
    }

    /// <summary>
    /// Generates an aggregate report for a specific time period.
    /// </summary>
    /// <param name="userId">The Telegram user ID.</param>
    /// <param name="startDate">The start date of the period (UTC).</param>
    /// <param name="endDate">The end date of the period (UTC).</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>An aggregate report with totals for the period.</returns>
    public async Task<PeriodAggregate> GetPeriodAggregateAsync(
        long userId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var sessions = await trackingSessionRepository.GetSessionsInRangeAsync(
            userId,
            startDate,
            endDate,
            cancellationToken);

        decimal totalWorkHours = 0;
        decimal totalCommuteHours = 0;
        decimal totalLunchHours = 0;

        var workDays = sessions
            .Where(s => s.State == TrackingState.Working)
            .Select(s => s.StartedAt.Date)
            .Distinct()
            .Count();

        foreach (var session in sessions)
        {
            if (session.EndedAt == null) continue;

            var duration = (decimal)(session.EndedAt.Value - session.StartedAt).TotalHours;

            switch (session.State)
            {
                case TrackingState.Working:
                    totalWorkHours += duration;
                    break;

                case TrackingState.Commuting:
                    totalCommuteHours += duration;
                    break;

                case TrackingState.Lunch:
                    totalLunchHours += duration;
                    break;
            }
        }

        // Calculate total duration from first to last activity
        decimal? totalDurationHours = null;
        if (sessions.Any())
        {
            var firstActivityStart = sessions.Min(s => s.StartedAt);

            // For last activity end time, consider active sessions (use current time if EndedAt is null)
            var lastActivityEnd = sessions.Max(s => s.EndedAt ?? DateTime.UtcNow);

            totalDurationHours = (decimal)(lastActivityEnd - firstActivityStart).TotalHours;
        }

        return new PeriodAggregate
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalWorkHours = totalWorkHours,
            TotalCommuteHours = totalCommuteHours,
            TotalLunchHours = totalLunchHours,
            WorkDaysCount = workDays,
            TotalDurationHours = totalDurationHours
        };
    }
}
