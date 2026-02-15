using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeSheet.Core.Application.Interfaces.Services;
using TimeSheet.Core.Domain.Enums;
using TimeSheet.Core.Domain.Repositories;
using TimeSheet.Presentation.API.Models.Analytics;

namespace TimeSheet.Presentation.API.Controllers;

/// <summary>
/// Analytics and reporting endpoints for work hour statistics, patterns, and chart data.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly ITrackingSessionRepository _sessionRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(
        ITrackingSessionRepository sessionRepository,
        IJwtTokenService jwtTokenService,
        ILogger<AnalyticsController> logger)
    {
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    /// <summary>
    /// Gets daily average statistics for a time period.
    /// Calculates average work hours, commute time, lunch duration, and total time at work.
    /// </summary>
    /// <param name="startDate">Start date of the period (UTC). Defaults to 30 days ago.</param>
    /// <param name="endDate">End date of the period (UTC). Defaults to today.</param>
    /// <returns>Daily averages report including work, commute, and lunch statistics.</returns>
    /// <response code="200">Daily averages retrieved successfully.</response>
    /// <response code="400">Invalid date range.</response>
    /// <response code="401">Unauthorized - invalid or missing JWT token.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("daily-averages")]
    [ProducesResponseType(typeof(DailyAveragesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DailyAveragesDto>> GetDailyAverages(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = _jwtTokenService.GetUserIdFromToken(User);

            // Default to last 30 days if not specified
            var start = startDate ?? DateTime.UtcNow.AddDays(-30).Date;
            var end = endDate ?? DateTime.UtcNow.Date.AddDays(1);

            if (start >= end)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid Request",
                    detail: "Start date must be before end date");
            }

            var sessions = await _sessionRepository.GetSessionsInRangeAsync(userId, start, end, cancellationToken);

            // Group sessions by date
            var sessionsByDate = sessions
                .Where(s => s.EndedAt.HasValue) // Only completed sessions
                .GroupBy(s => s.StartedAt.Date)
                .ToList();

            var daysIncluded = sessionsByDate.Count;
            var workDays = sessionsByDate.Count(g => g.Any(s => s.State == TrackingState.Working));

            if (daysIncluded == 0)
            {
                return Ok(new DailyAveragesDto
                {
                    DaysIncluded = 0,
                    AverageWorkHours = 0,
                    AverageCommuteToWorkHours = 0,
                    AverageCommuteToHomeHours = 0,
                    AverageLunchHours = 0,
                    TotalWorkDays = 0,
                    AverageTotalDurationHours = 0
                });
            }

            decimal totalWorkHours = 0;
            decimal totalCommuteToWorkHours = 0;
            decimal totalCommuteToHomeHours = 0;
            decimal totalLunchHours = 0;
            decimal totalDurationHours = 0;

            foreach (var dayGroup in sessionsByDate)
            {
                var daySessions = dayGroup.ToList();

                foreach (var session in daySessions)
                {
                    var duration = (decimal)(session.EndedAt!.Value - session.StartedAt).TotalHours;

                    switch (session.State)
                    {
                        case TrackingState.Working:
                            totalWorkHours += duration;
                            break;
                        case TrackingState.Commuting when session.CommuteDirection == CommuteDirection.ToWork:
                            totalCommuteToWorkHours += duration;
                            break;
                        case TrackingState.Commuting when session.CommuteDirection == CommuteDirection.ToHome:
                            totalCommuteToHomeHours += duration;
                            break;
                        case TrackingState.Lunch:
                            totalLunchHours += duration;
                            break;
                    }
                }

                // Calculate total duration for this day (first to last activity)
                if (daySessions.Any())
                {
                    var firstActivity = daySessions.Min(s => s.StartedAt);
                    var lastActivity = daySessions.Max(s => s.EndedAt!.Value);
                    totalDurationHours += (decimal)(lastActivity - firstActivity).TotalHours;
                }
            }

            return Ok(new DailyAveragesDto
            {
                DaysIncluded = daysIncluded,
                AverageWorkHours = daysIncluded > 0 ? totalWorkHours / daysIncluded : 0,
                AverageCommuteToWorkHours = daysIncluded > 0 ? totalCommuteToWorkHours / daysIncluded : 0,
                AverageCommuteToHomeHours = daysIncluded > 0 ? totalCommuteToHomeHours / daysIncluded : 0,
                AverageLunchHours = daysIncluded > 0 ? totalLunchHours / daysIncluded : 0,
                TotalWorkDays = workDays,
                AverageTotalDurationHours = daysIncluded > 0 ? totalDurationHours / daysIncluded : 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating daily averages");
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An error occurred while calculating daily averages");
        }
    }

    /// <summary>
    /// Gets commute pattern analysis by day of week.
    /// Identifies optimal commute times for each day based on historical data.
    /// </summary>
    /// <param name="direction">Commute direction (ToWork or ToHome).</param>
    /// <param name="startDate">Start date for analysis (UTC). Defaults to 90 days ago.</param>
    /// <param name="endDate">End date for analysis (UTC). Defaults to today.</param>
    /// <returns>List of commute patterns for each day of the week.</returns>
    /// <response code="200">Commute patterns retrieved successfully.</response>
    /// <response code="400">Invalid parameters.</response>
    /// <response code="401">Unauthorized - invalid or missing JWT token.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("commute-patterns")]
    [ProducesResponseType(typeof(List<CommutePatternsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<CommutePatternsDto>>> GetCommutePatterns(
        [FromQuery] string direction,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = _jwtTokenService.GetUserIdFromToken(User);

            // Parse direction
            if (!Enum.TryParse<CommuteDirection>(direction, true, out var commuteDirection))
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid Request",
                    detail: "Invalid direction. Use 'ToWork' or 'ToHome'");
            }

            // Default to last 90 days if not specified
            var start = startDate ?? DateTime.UtcNow.AddDays(-90).Date;
            var end = endDate ?? DateTime.UtcNow.Date.AddDays(1);

            if (start >= end)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid Request",
                    detail: "Start date must be before end date");
            }

            var sessions = await _sessionRepository.GetSessionsInRangeAsync(userId, start, end, cancellationToken);

            // Filter commute sessions by direction
            var commuteSessions = sessions
                .Where(s => s.State == TrackingState.Commuting
                         && s.CommuteDirection == commuteDirection
                         && s.EndedAt.HasValue)
                .ToList();

            // Group by day of week
            var patternsByDayOfWeek = commuteSessions
                .GroupBy(s => s.StartedAt.DayOfWeek)
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    var sessionsForDay = g.ToList();
                    var averageDuration = (decimal)sessionsForDay.Average(s => (s.EndedAt!.Value - s.StartedAt).TotalHours);

                    // Find optimal start hour by grouping by hour and finding shortest average
                    var byHour = sessionsForDay
                        .GroupBy(s => s.StartedAt.Hour)
                        .Select(h => new
                        {
                            Hour = h.Key,
                            AvgDuration = (decimal)h.Average(s => (s.EndedAt!.Value - s.StartedAt).TotalHours)
                        })
                        .OrderBy(h => h.AvgDuration)
                        .FirstOrDefault();

                    return new CommutePatternsDto
                    {
                        DayOfWeek = g.Key,
                        AverageDurationHours = averageDuration,
                        OptimalStartHour = byHour?.Hour,
                        ShortestDurationHours = byHour?.AvgDuration,
                        SessionCount = sessionsForDay.Count
                    };
                })
                .ToList();

            // Fill in missing days of week with zero data
            var allDays = Enum.GetValues<DayOfWeek>();
            var result = allDays.Select(day =>
            {
                var existing = patternsByDayOfWeek.FirstOrDefault(p => p.DayOfWeek == day);
                return existing ?? new CommutePatternsDto
                {
                    DayOfWeek = day,
                    AverageDurationHours = 0,
                    OptimalStartHour = null,
                    ShortestDurationHours = null,
                    SessionCount = 0
                };
            }).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating commute patterns");
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An error occurred while calculating commute patterns");
        }
    }

    /// <summary>
    /// Gets aggregated totals for a specific time period.
    /// Calculates total work hours, commute time, lunch time, and work days.
    /// </summary>
    /// <param name="startDate">Start date of the period (UTC).</param>
    /// <param name="endDate">End date of the period (UTC).</param>
    /// <returns>Aggregated statistics for the period.</returns>
    /// <response code="200">Period aggregate retrieved successfully.</response>
    /// <response code="400">Invalid date range.</response>
    /// <response code="401">Unauthorized - invalid or missing JWT token.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("period-aggregate")]
    [ProducesResponseType(typeof(PeriodAggregateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PeriodAggregateDto>> GetPeriodAggregate(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = _jwtTokenService.GetUserIdFromToken(User);

            if (startDate >= endDate)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid Request",
                    detail: "Start date must be before end date");
            }

            var sessions = await _sessionRepository.GetSessionsInRangeAsync(userId, startDate, endDate, cancellationToken);

            // Only include completed sessions
            var completedSessions = sessions.Where(s => s.EndedAt.HasValue).ToList();

            decimal totalWorkHours = 0;
            decimal totalCommuteHours = 0;
            decimal totalLunchHours = 0;

            foreach (var session in completedSessions)
            {
                var duration = (decimal)(session.EndedAt!.Value - session.StartedAt).TotalHours;

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

            // Count work days (days with at least one work session)
            var workDays = completedSessions
                .Where(s => s.State == TrackingState.Working)
                .Select(s => s.StartedAt.Date)
                .Distinct()
                .Count();

            // Calculate total duration from first to last activity
            decimal? totalDurationHours = null;
            if (completedSessions.Any())
            {
                var firstActivity = completedSessions.Min(s => s.StartedAt);
                var lastActivity = completedSessions.Max(s => s.EndedAt!.Value);
                totalDurationHours = (decimal)(lastActivity - firstActivity).TotalHours;
            }

            return Ok(new PeriodAggregateDto
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalWorkHours = totalWorkHours,
                TotalCommuteHours = totalCommuteHours,
                TotalLunchHours = totalLunchHours,
                WorkDaysCount = workDays,
                TotalDurationHours = totalDurationHours
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating period aggregate");
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An error occurred while calculating period aggregate");
        }
    }

    /// <summary>
    /// Gets daily breakdown data for a time period.
    /// Returns per-day statistics suitable for table display.
    /// </summary>
    /// <param name="startDate">Start date of the period (UTC).</param>
    /// <param name="endDate">End date of the period (UTC).</param>
    /// <returns>List of daily breakdown rows.</returns>
    /// <response code="200">Daily breakdown retrieved successfully.</response>
    /// <response code="400">Invalid date range.</response>
    /// <response code="401">Unauthorized - invalid or missing JWT token.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("daily-breakdown")]
    [ProducesResponseType(typeof(List<DailyBreakdownDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<DailyBreakdownDto>>> GetDailyBreakdown(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = _jwtTokenService.GetUserIdFromToken(User);

            if (startDate >= endDate)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid Request",
                    detail: "Start date must be before end date");
            }

            var sessions = await _sessionRepository.GetSessionsInRangeAsync(userId, startDate, endDate, cancellationToken);

            // Group by date
            var sessionsByDate = sessions
                .Where(s => s.EndedAt.HasValue)
                .GroupBy(s => s.StartedAt.Date)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Generate breakdown for each day in range
            var result = new List<DailyBreakdownDto>();
            for (var date = startDate.Date; date < endDate.Date; date = date.AddDays(1))
            {
                if (sessionsByDate.TryGetValue(date, out var daySessions))
                {
                    decimal workHours = 0;
                    decimal commuteToWorkHours = 0;
                    decimal commuteToHomeHours = 0;
                    decimal lunchHours = 0;

                    foreach (var session in daySessions)
                    {
                        var duration = (decimal)(session.EndedAt!.Value - session.StartedAt).TotalHours;

                        switch (session.State)
                        {
                            case TrackingState.Working:
                                workHours += duration;
                                break;
                            case TrackingState.Commuting when session.CommuteDirection == CommuteDirection.ToWork:
                                commuteToWorkHours += duration;
                                break;
                            case TrackingState.Commuting when session.CommuteDirection == CommuteDirection.ToHome:
                                commuteToHomeHours += duration;
                                break;
                            case TrackingState.Lunch:
                                lunchHours += duration;
                                break;
                        }
                    }

                    // Calculate total duration (first to last activity)
                    decimal? totalDurationHours = null;
                    if (daySessions.Any())
                    {
                        var firstActivity = daySessions.Min(s => s.StartedAt);
                        var lastActivity = daySessions.Max(s => s.EndedAt!.Value);
                        totalDurationHours = (decimal)(lastActivity - firstActivity).TotalHours;
                    }

                    result.Add(new DailyBreakdownDto
                    {
                        Date = date,
                        WorkHours = workHours,
                        CommuteToWorkHours = commuteToWorkHours,
                        CommuteToHomeHours = commuteToHomeHours,
                        LunchHours = lunchHours,
                        TotalDurationHours = totalDurationHours,
                        HasActivity = true
                    });
                }
                else
                {
                    // No activity on this day
                    result.Add(new DailyBreakdownDto
                    {
                        Date = date,
                        WorkHours = 0,
                        CommuteToWorkHours = 0,
                        CommuteToHomeHours = 0,
                        LunchHours = 0,
                        TotalDurationHours = null,
                        HasActivity = false
                    });
                }
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating daily breakdown");
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An error occurred while calculating daily breakdown");
        }
    }

    /// <summary>
    /// Gets chart data for visualization including idle time.
    /// Returns data points for work, commute, lunch, idle time, and total duration.
    /// </summary>
    /// <param name="startDate">Start date of the period (UTC).</param>
    /// <param name="endDate">End date of the period (UTC).</param>
    /// <param name="groupBy">Grouping mode (Day, Week, Month, Year). Defaults to Day.</param>
    /// <returns>Chart data with labels and data points.</returns>
    /// <response code="200">Chart data retrieved successfully.</response>
    /// <response code="400">Invalid date range or grouping mode.</response>
    /// <response code="401">Unauthorized - invalid or missing JWT token.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("chart-data")]
    [ProducesResponseType(typeof(ChartDataDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ChartDataDto>> GetChartData(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] string groupBy = "Day",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = _jwtTokenService.GetUserIdFromToken(User);

            if (startDate >= endDate)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid Request",
                    detail: "Start date must be before end date");
            }

            // Validate groupBy parameter
            var validGroupings = new[] { "Day", "Week", "Month", "Year" };
            if (!validGroupings.Contains(groupBy, StringComparer.OrdinalIgnoreCase))
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid Request",
                    detail: "Invalid groupBy parameter. Use 'Day', 'Week', 'Month', or 'Year'");
            }

            var sessions = await _sessionRepository.GetSessionsInRangeAsync(userId, startDate, endDate, cancellationToken);
            var completedSessions = sessions.Where(s => s.EndedAt.HasValue).ToList();

            // Group sessions based on groupBy parameter
            var groupedData = GroupSessionsByPeriod(completedSessions, startDate, endDate, groupBy);

            var labels = new List<string>();
            var workHours = new List<decimal>();
            var commuteHours = new List<decimal>();
            var lunchHours = new List<decimal>();
            var idleHours = new List<decimal>();
            var totalDurationHours = new List<decimal>();

            foreach (var group in groupedData)
            {
                labels.Add(group.Label);

                decimal work = 0, commute = 0, lunch = 0;

                foreach (var session in group.Sessions)
                {
                    var duration = (decimal)(session.EndedAt!.Value - session.StartedAt).TotalHours;

                    switch (session.State)
                    {
                        case TrackingState.Working:
                            work += duration;
                            break;
                        case TrackingState.Commuting:
                            commute += duration;
                            break;
                        case TrackingState.Lunch:
                            lunch += duration;
                            break;
                    }
                }

                // Calculate total duration and idle time
                decimal totalDuration = 0;
                decimal idle = 0;

                if (group.Sessions.Any())
                {
                    var firstActivity = group.Sessions.Min(s => s.StartedAt);
                    var lastActivity = group.Sessions.Max(s => s.EndedAt!.Value);
                    totalDuration = (decimal)(lastActivity - firstActivity).TotalHours;

                    // Idle time = total duration - (work + commute + lunch)
                    // This represents gaps between sessions
                    idle = Math.Max(0, totalDuration - (work + commute + lunch));
                }

                workHours.Add(work);
                commuteHours.Add(commute);
                lunchHours.Add(lunch);
                idleHours.Add(idle);
                totalDurationHours.Add(totalDuration);
            }

            return Ok(new ChartDataDto
            {
                Labels = labels,
                WorkHours = workHours,
                CommuteHours = commuteHours,
                LunchHours = lunchHours,
                IdleHours = idleHours,
                TotalDurationHours = totalDurationHours
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating chart data");
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An error occurred while generating chart data");
        }
    }

    private static List<ChartDataGroup> GroupSessionsByPeriod(
        List<Core.Domain.Entities.TrackingSession> sessions,
        DateTime startDate,
        DateTime endDate,
        string groupBy)
    {
        return groupBy.ToLower() switch
        {
            "day" => GroupByDay(sessions, startDate, endDate),
            "week" => GroupByWeek(sessions, startDate, endDate),
            "month" => GroupByMonth(sessions, startDate, endDate),
            "year" => GroupByYear(sessions, startDate, endDate),
            _ => GroupByDay(sessions, startDate, endDate)
        };
    }

    private static List<ChartDataGroup> GroupByDay(
        List<Core.Domain.Entities.TrackingSession> sessions,
        DateTime startDate,
        DateTime endDate)
    {
        var result = new List<ChartDataGroup>();
        var sessionsByDate = sessions.GroupBy(s => s.StartedAt.Date).ToDictionary(g => g.Key, g => g.ToList());

        for (var date = startDate.Date; date < endDate.Date; date = date.AddDays(1))
        {
            result.Add(new ChartDataGroup
            {
                Label = date.ToString("yyyy-MM-dd"),
                Sessions = sessionsByDate.TryGetValue(date, out var daySessions) ? daySessions : []
            });
        }

        return result;
    }

    private static List<ChartDataGroup> GroupByWeek(
        List<Core.Domain.Entities.TrackingSession> sessions,
        DateTime startDate,
        DateTime endDate)
    {
        var result = new List<ChartDataGroup>();
        var sessionsByWeek = sessions
            .GroupBy(s => GetWeekStartDate(s.StartedAt))
            .ToDictionary(g => g.Key, g => g.ToList());

        for (var date = GetWeekStartDate(startDate); date < endDate; date = date.AddDays(7))
        {
            var weekEnd = date.AddDays(7);
            result.Add(new ChartDataGroup
            {
                Label = $"{date:yyyy-MM-dd} to {weekEnd.AddDays(-1):yyyy-MM-dd}",
                Sessions = sessionsByWeek.TryGetValue(date, out var weekSessions) ? weekSessions : []
            });
        }

        return result;
    }

    private static List<ChartDataGroup> GroupByMonth(
        List<Core.Domain.Entities.TrackingSession> sessions,
        DateTime startDate,
        DateTime endDate)
    {
        var result = new List<ChartDataGroup>();
        var sessionsByMonth = sessions
            .GroupBy(s => new DateTime(s.StartedAt.Year, s.StartedAt.Month, 1))
            .ToDictionary(g => g.Key, g => g.ToList());

        var current = new DateTime(startDate.Year, startDate.Month, 1);
        var end = new DateTime(endDate.Year, endDate.Month, 1);

        while (current <= end)
        {
            result.Add(new ChartDataGroup
            {
                Label = current.ToString("yyyy-MM"),
                Sessions = sessionsByMonth.TryGetValue(current, out var monthSessions) ? monthSessions : []
            });
            current = current.AddMonths(1);
        }

        return result;
    }

    private static List<ChartDataGroup> GroupByYear(
        List<Core.Domain.Entities.TrackingSession> sessions,
        DateTime startDate,
        DateTime endDate)
    {
        var result = new List<ChartDataGroup>();
        var sessionsByYear = sessions
            .GroupBy(s => s.StartedAt.Year)
            .ToDictionary(g => g.Key, g => g.ToList());

        for (var year = startDate.Year; year <= endDate.Year; year++)
        {
            result.Add(new ChartDataGroup
            {
                Label = year.ToString(),
                Sessions = sessionsByYear.TryGetValue(year, out var yearSessions) ? yearSessions : []
            });
        }

        return result;
    }

    private static DateTime GetWeekStartDate(DateTime date)
    {
        // ISO 8601: Week starts on Monday
        var dayOfWeek = date.DayOfWeek;
        var diff = dayOfWeek == DayOfWeek.Sunday ? -6 : 1 - (int)dayOfWeek;
        return date.Date.AddDays(diff);
    }

    private sealed class ChartDataGroup
    {
        public required string Label { get; init; }
        public required List<Core.Domain.Entities.TrackingSession> Sessions { get; init; }
    }
}
