using Moq;
using TimeSheet.Core.Application.Interfaces.Services;
using TimeSheet.Core.Application.Interfaces.Persistence;
using TimeSheet.Core.Domain.Repositories;
using TimeSheet.Core.Application.Services;
using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.Enums;

namespace TimeSheet.Tests.Unit.Services;

public class ReportingServiceTests
{
    private readonly Mock<ITrackingSessionRepository> _mockTrackingSessionRepository;
    private readonly ReportingService _sut;

    public ReportingServiceTests()
    {
        _mockTrackingSessionRepository = new Mock<ITrackingSessionRepository>();
        _sut = new ReportingService(_mockTrackingSessionRepository.Object);
    }

    [Fact]
    public async Task GetDailyAveragesAsync_WithNoSessions_ReturnsZeroAverages()
    {
        // Arrange
        const long userId = 123456;
        const int days = 7;

        _mockTrackingSessionRepository
            .Setup(x => x.GetSessionsInRangeAsync(userId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrackingSession>());

        // Act
        var result = await _sut.GetDailyAveragesAsync(userId, days, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.DaysIncluded);
        Assert.Equal(0, result.AverageWorkHours);
        Assert.Equal(0, result.AverageCommuteToWorkHours);
        Assert.Equal(0, result.AverageCommuteToHomeHours);
        Assert.Equal(0, result.AverageLunchHours);
        Assert.Equal(0, result.TotalWorkDays);
    }

    [Fact]
    public async Task GetDailyAveragesAsync_WithOneWorkDay_CalculatesCorrectAverages()
    {
        // Arrange
        const long userId = 123456;
        const int days = 7;
        var baseDate = DateTime.UtcNow.Date;

        var sessions = new List<TrackingSession>
        {
            new(userId, TrackingState.Commuting, baseDate.AddHours(8), CommuteDirection.ToWork),
            new(userId, TrackingState.Working, baseDate.AddHours(8.5)),
            new(userId, TrackingState.Lunch, baseDate.AddHours(12)),
            new(userId, TrackingState.Working, baseDate.AddHours(13)),
            new(userId, TrackingState.Commuting, baseDate.AddHours(17), CommuteDirection.ToHome)
        };

        // End the sessions
        sessions[0].End(baseDate.AddHours(8.5)); // 0.5 hours commute to work
        sessions[1].End(baseDate.AddHours(12));  // 3.5 hours work
        sessions[2].End(baseDate.AddHours(13));  // 1 hour lunch
        sessions[3].End(baseDate.AddHours(17));  // 4 hours work
        sessions[4].End(baseDate.AddHours(17.5)); // 0.5 hours commute to home

        _mockTrackingSessionRepository
            .Setup(x => x.GetSessionsInRangeAsync(userId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        // Act
        var result = await _sut.GetDailyAveragesAsync(userId, days, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.DaysIncluded);
        Assert.Equal(1, result.TotalWorkDays);
        Assert.Equal(7.5m, result.AverageWorkHours); // 3.5 + 4 = 7.5 hours
        Assert.Equal(0.5m, result.AverageCommuteToWorkHours);
        Assert.Equal(0.5m, result.AverageCommuteToHomeHours);
        Assert.Equal(1m, result.AverageLunchHours);
        Assert.Equal(9.5m, result.AverageTotalDurationHours); // 8:00 to 17:30 = 9.5 hours
    }

    [Fact]
    public async Task GetDailyAveragesAsync_WithMultipleWorkDays_CalculatesCorrectAverages()
    {
        // Arrange
        const long userId = 123456;
        const int days = 7;
        var baseDate = DateTime.UtcNow.Date;

        var sessions = new List<TrackingSession>
        {
            // Day 1
            new(userId, TrackingState.Working, baseDate.AddHours(9)),
            // Day 2
            new(userId, TrackingState.Working, baseDate.AddDays(1).AddHours(9)),
        };

        sessions[0].End(baseDate.AddHours(17)); // 8 hours work
        sessions[1].End(baseDate.AddDays(1).AddHours(15)); // 6 hours work

        _mockTrackingSessionRepository
            .Setup(x => x.GetSessionsInRangeAsync(userId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        // Act
        var result = await _sut.GetDailyAveragesAsync(userId, days, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.DaysIncluded);
        Assert.Equal(2, result.TotalWorkDays);
        Assert.Equal(7m, result.AverageWorkHours); // (8 + 6) / 2 = 7 hours
        Assert.Equal(7m, result.AverageTotalDurationHours); // Day 1: 8h, Day 2: 6h, avg = 7h
    }

    [Fact]
    public async Task GetDailyAveragesAsync_CalculatesTotalDurationPerDay()
    {
        // Arrange
        const long userId = 123456;
        const int days = 7;
        var baseDate = DateTime.UtcNow.Date;

        var sessions = new List<TrackingSession>
        {
            // Day 1: 8:00 to 17:30 (9.5 hours total duration)
            new(userId, TrackingState.Commuting, baseDate.AddHours(8), CommuteDirection.ToWork),
            new(userId, TrackingState.Working, baseDate.AddHours(9)),
            new(userId, TrackingState.Lunch, baseDate.AddHours(12)),
            new(userId, TrackingState.Working, baseDate.AddHours(13)),
            new(userId, TrackingState.Commuting, baseDate.AddHours(17), CommuteDirection.ToHome),

            // Day 2: 7:00 to 16:00 (9 hours total duration)
            new(userId, TrackingState.Commuting, baseDate.AddDays(1).AddHours(7), CommuteDirection.ToWork),
            new(userId, TrackingState.Working, baseDate.AddDays(1).AddHours(8)),
            new(userId, TrackingState.Working, baseDate.AddDays(1).AddHours(14)),
        };

        sessions[0].End(baseDate.AddHours(9));
        sessions[1].End(baseDate.AddHours(12));
        sessions[2].End(baseDate.AddHours(13));
        sessions[3].End(baseDate.AddHours(17));
        sessions[4].End(baseDate.AddHours(17.5));
        sessions[5].End(baseDate.AddDays(1).AddHours(8));
        sessions[6].End(baseDate.AddDays(1).AddHours(14));
        sessions[7].End(baseDate.AddDays(1).AddHours(16));

        _mockTrackingSessionRepository
            .Setup(x => x.GetSessionsInRangeAsync(userId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        // Act
        var result = await _sut.GetDailyAveragesAsync(userId, days, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.DaysIncluded);
        Assert.Equal(2, result.TotalWorkDays);
        // Average total duration: (9.5 + 9) / 2 = 9.25 hours
        Assert.Equal(9.25m, result.AverageTotalDurationHours);
    }

    [Fact]
    public async Task GetCommutePatternsAsync_WithNoSessions_ReturnsEmptyList()
    {
        // Arrange
        const long userId = 123456;

        _mockTrackingSessionRepository
            .Setup(x => x.GetSessionsInRangeAsync(userId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrackingSession>());

        // Act
        var result = await _sut.GetCommutePatternsAsync(userId, CommuteDirection.ToWork, CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCommutePatternsAsync_WithCommuteSessions_CalculatesPatternsPerDayOfWeek()
    {
        // Arrange
        const long userId = 123456;
        var baseDate = new DateTime(2026, 2, 2); // A Monday

        var sessions = new List<TrackingSession>
        {
            // Monday at 8:00
            new(userId, TrackingState.Commuting, baseDate.AddHours(8), CommuteDirection.ToWork),
            // Monday at 8:00 (another week)
            new(userId, TrackingState.Commuting, baseDate.AddDays(7).AddHours(8), CommuteDirection.ToWork),
            // Tuesday at 9:00
            new(userId, TrackingState.Commuting, baseDate.AddDays(1).AddHours(9), CommuteDirection.ToWork),
        };

        sessions[0].End(baseDate.AddHours(8.5)); // 0.5 hours
        sessions[1].End(baseDate.AddDays(7).AddHours(8.75)); // 0.75 hours
        sessions[2].End(baseDate.AddDays(1).AddHours(9.5)); // 0.5 hours

        _mockTrackingSessionRepository
            .Setup(x => x.GetSessionsInRangeAsync(userId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        // Act
        var result = await _sut.GetCommutePatternsAsync(userId, CommuteDirection.ToWork, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);

        var monday = result.First(p => p.DayOfWeek == DayOfWeek.Monday);
        Assert.Equal(2, monday.SessionCount);
        Assert.Equal(0.625m, monday.AverageDurationHours); // (0.5 + 0.75) / 2
        Assert.Equal(8, monday.OptimalStartHour); // Hour with shortest average
        Assert.Equal(0.625m, monday.ShortestDurationHours);

        var tuesday = result.First(p => p.DayOfWeek == DayOfWeek.Tuesday);
        Assert.Equal(1, tuesday.SessionCount);
        Assert.Equal(0.5m, tuesday.AverageDurationHours);
        Assert.Null(tuesday.OptimalStartHour); // Only 1 session at this hour, need 2+ for optimal
    }

    [Fact]
    public async Task GetCommutePatternsAsync_IdentifiesOptimalStartHour()
    {
        // Arrange
        const long userId = 123456;
        var baseDate = new DateTime(2026, 2, 2); // A Monday

        var sessions = new List<TrackingSession>
        {
            // Monday at 7:00 - fast commute
            new(userId, TrackingState.Commuting, baseDate.AddHours(7), CommuteDirection.ToWork),
            new(userId, TrackingState.Commuting, baseDate.AddDays(7).AddHours(7), CommuteDirection.ToWork),

            // Monday at 8:00 - slow commute
            new(userId, TrackingState.Commuting, baseDate.AddHours(8), CommuteDirection.ToWork),
            new(userId, TrackingState.Commuting, baseDate.AddDays(7).AddHours(8), CommuteDirection.ToWork),
        };

        sessions[0].End(baseDate.AddHours(7.25)); // 0.25 hours
        sessions[1].End(baseDate.AddDays(7).AddHours(7.25)); // 0.25 hours
        sessions[2].End(baseDate.AddHours(8.75)); // 0.75 hours
        sessions[3].End(baseDate.AddDays(7).AddHours(9)); // 1 hour

        _mockTrackingSessionRepository
            .Setup(x => x.GetSessionsInRangeAsync(userId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        // Act
        var result = await _sut.GetCommutePatternsAsync(userId, CommuteDirection.ToWork, CancellationToken.None);

        // Assert
        var monday = result.First(p => p.DayOfWeek == DayOfWeek.Monday);
        Assert.Equal(7, monday.OptimalStartHour); // 7:00 has shorter average
        Assert.Equal(0.25m, monday.ShortestDurationHours);
    }

    [Fact]
    public async Task GetPeriodAggregateAsync_WithNoSessions_ReturnsZeroTotals()
    {
        // Arrange
        const long userId = 123456;
        var startDate = DateTime.UtcNow.Date.AddDays(-7);
        var endDate = DateTime.UtcNow.Date;

        _mockTrackingSessionRepository
            .Setup(x => x.GetSessionsInRangeAsync(userId, startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrackingSession>());

        // Act
        var result = await _sut.GetPeriodAggregateAsync(userId, startDate, endDate, CancellationToken.None);

        // Assert
        Assert.Equal(startDate, result.StartDate);
        Assert.Equal(endDate, result.EndDate);
        Assert.Equal(0, result.TotalWorkHours);
        Assert.Equal(0, result.TotalCommuteHours);
        Assert.Equal(0, result.TotalLunchHours);
        Assert.Equal(0, result.WorkDaysCount);
    }

    [Fact]
    public async Task GetPeriodAggregateAsync_WithSessions_CalculatesTotals()
    {
        // Arrange
        const long userId = 123456;
        var startDate = DateTime.UtcNow.Date.AddDays(-7);
        var endDate = DateTime.UtcNow.Date;
        var baseDate = DateTime.UtcNow.Date.AddDays(-1);

        var sessions = new List<TrackingSession>
        {
            new(userId, TrackingState.Commuting, baseDate.AddHours(8), CommuteDirection.ToWork),
            new(userId, TrackingState.Working, baseDate.AddHours(9)),
            new(userId, TrackingState.Lunch, baseDate.AddHours(12)),
            new(userId, TrackingState.Working, baseDate.AddHours(13)),
            new(userId, TrackingState.Commuting, baseDate.AddHours(17), CommuteDirection.ToHome),

            // Another day
            new(userId, TrackingState.Working, baseDate.AddDays(-1).AddHours(9)),
        };

        sessions[0].End(baseDate.AddHours(9));   // 1 hour commute
        sessions[1].End(baseDate.AddHours(12));  // 3 hours work
        sessions[2].End(baseDate.AddHours(13));  // 1 hour lunch
        sessions[3].End(baseDate.AddHours(17));  // 4 hours work
        sessions[4].End(baseDate.AddHours(18));  // 1 hour commute
        sessions[5].End(baseDate.AddDays(-1).AddHours(15)); // 6 hours work

        _mockTrackingSessionRepository
            .Setup(x => x.GetSessionsInRangeAsync(userId, startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        // Act
        var result = await _sut.GetPeriodAggregateAsync(userId, startDate, endDate, CancellationToken.None);

        // Assert
        Assert.Equal(13m, result.TotalWorkHours); // 3 + 4 + 6
        Assert.Equal(2m, result.TotalCommuteHours); // 1 + 1
        Assert.Equal(1m, result.TotalLunchHours);
        Assert.Equal(2, result.WorkDaysCount); // 2 distinct days with work sessions
    }

    [Fact]
    public async Task GetDailyAveragesAsync_OnlyProcessesCompletedSessions()
    {
        // Arrange
        const long userId = 123456;
        const int days = 7;
        var baseDate = DateTime.UtcNow.Date;

        // Repository only returns completed sessions (EndedAt != null)
        var sessions = new List<TrackingSession>
        {
            new(userId, TrackingState.Working, baseDate.AddHours(9)),
        };

        sessions[0].End(baseDate.AddHours(17)); // 8 hours work

        _mockTrackingSessionRepository
            .Setup(x => x.GetSessionsInRangeAsync(userId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        // Act
        var result = await _sut.GetDailyAveragesAsync(userId, days, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.DaysIncluded);
        Assert.Equal(1, result.TotalWorkDays);
        Assert.Equal(8m, result.AverageWorkHours);
    }

    [Fact]
    public async Task GetPeriodAggregateAsync_CalculatesTotalDurationFromFirstToLastActivity()
    {
        // Arrange
        const long userId = 123456;
        var startDate = DateTime.UtcNow.Date.AddDays(-7);
        var endDate = DateTime.UtcNow.Date;
        var baseDate = DateTime.UtcNow.Date.AddDays(-1);

        var sessions = new List<TrackingSession>
        {
            // First activity: commute at 8:00
            new(userId, TrackingState.Commuting, baseDate.AddHours(8), CommuteDirection.ToWork),
            new(userId, TrackingState.Working, baseDate.AddHours(9)),
            new(userId, TrackingState.Lunch, baseDate.AddHours(12)),
            new(userId, TrackingState.Working, baseDate.AddHours(13)),
            // Last activity: commute ending at 17:30
            new(userId, TrackingState.Commuting, baseDate.AddHours(17), CommuteDirection.ToHome),
        };

        sessions[0].End(baseDate.AddHours(9));     // 8:00 - 9:00
        sessions[1].End(baseDate.AddHours(12));    // 9:00 - 12:00
        sessions[2].End(baseDate.AddHours(13));    // 12:00 - 13:00
        sessions[3].End(baseDate.AddHours(17));    // 13:00 - 17:00
        sessions[4].End(baseDate.AddHours(17.5));  // 17:00 - 17:30

        _mockTrackingSessionRepository
            .Setup(x => x.GetSessionsInRangeAsync(userId, startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        // Act
        var result = await _sut.GetPeriodAggregateAsync(userId, startDate, endDate, CancellationToken.None);

        // Assert
        // Total duration should be from 8:00 to 17:30 = 9.5 hours
        Assert.Equal(9.5m, result.TotalDurationHours);
    }

    [Fact]
    public async Task GetPeriodAggregateAsync_TotalDurationIsNullWhenNoSessions()
    {
        // Arrange
        const long userId = 123456;
        var startDate = DateTime.UtcNow.Date.AddDays(-7);
        var endDate = DateTime.UtcNow.Date;

        _mockTrackingSessionRepository
            .Setup(x => x.GetSessionsInRangeAsync(userId, startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrackingSession>());

        // Act
        var result = await _sut.GetPeriodAggregateAsync(userId, startDate, endDate, CancellationToken.None);

        // Assert
        Assert.Null(result.TotalDurationHours);
    }

    [Fact]
    public async Task GetPeriodAggregateAsync_TotalDurationHandlesActiveSessions()
    {
        // Arrange
        const long userId = 123456;
        var startDate = DateTime.UtcNow.Date;
        var endDate = DateTime.UtcNow.Date.AddDays(1);
        var baseDate = DateTime.UtcNow.Date;

        var sessions = new List<TrackingSession>
        {
            // First activity at 8:00
            new(userId, TrackingState.Commuting, baseDate.AddHours(8), CommuteDirection.ToWork),
            new(userId, TrackingState.Working, baseDate.AddHours(9)),
            // Active session (no EndedAt) - should use current time
            new(userId, TrackingState.Working, baseDate.AddHours(14)),
        };

        sessions[0].End(baseDate.AddHours(9)); // 8:00 - 9:00
        sessions[1].End(baseDate.AddHours(12)); // 9:00 - 12:00
        // sessions[2] is still active (EndedAt is null)

        _mockTrackingSessionRepository
            .Setup(x => x.GetSessionsInRangeAsync(userId, startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        // Act
        var result = await _sut.GetPeriodAggregateAsync(userId, startDate, endDate, CancellationToken.None);

        // Assert
        // Total duration should be from 8:00 to now (active session end)
        Assert.NotNull(result.TotalDurationHours);
        // Should be at least 6 hours (from 8:00 to 14:00)
        Assert.True(result.TotalDurationHours >= 6m);
    }

    [Fact]
    public async Task GetPeriodAggregateAsync_TotalDurationSpansMultipleDays()
    {
        // Arrange
        const long userId = 123456;
        var startDate = DateTime.UtcNow.Date.AddDays(-7);
        var endDate = DateTime.UtcNow.Date;
        var day1 = DateTime.UtcNow.Date.AddDays(-2);
        var day2 = DateTime.UtcNow.Date.AddDays(-1);

        var sessions = new List<TrackingSession>
        {
            // Day 1: First activity at 8:00
            new(userId, TrackingState.Working, day1.AddHours(8)),
            new(userId, TrackingState.Working, day1.AddHours(17)),
            // Day 2: Last activity ending at 17:30
            new(userId, TrackingState.Working, day2.AddHours(9)),
            new(userId, TrackingState.Working, day2.AddHours(14)),
        };

        sessions[0].End(day1.AddHours(12));
        sessions[1].End(day1.AddHours(18));
        sessions[2].End(day2.AddHours(12));
        sessions[3].End(day2.AddHours(17.5));

        _mockTrackingSessionRepository
            .Setup(x => x.GetSessionsInRangeAsync(userId, startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        // Act
        var result = await _sut.GetPeriodAggregateAsync(userId, startDate, endDate, CancellationToken.None);

        // Assert
        // Total duration should span from day1 8:00 to day2 17:30 = 33.5 hours
        Assert.Equal(33.5m, result.TotalDurationHours);
    }

    [Fact]
    public async Task GetDailyBreakdownAsync_WithNoSessions_ReturnsEmptyRowsForAllDays()
    {
        // Arrange
        const long userId = 123456;
        var startDate = DateTime.UtcNow.Date;
        var endDate = startDate.AddDays(7);

        _mockTrackingSessionRepository
            .Setup(x => x.GetSessionsInRangeAsync(userId, startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrackingSession>());

        // Act
        var result = await _sut.GetDailyBreakdownAsync(userId, startDate, endDate, CancellationToken.None);

        // Assert
        Assert.Equal(7, result.Count); // 7 days in range
        Assert.All(result, row =>
        {
            Assert.False(row.HasActivity);
            Assert.Equal(0, row.WorkHours);
            Assert.Equal(0, row.CommuteToWorkHours);
            Assert.Equal(0, row.CommuteToHomeHours);
            Assert.Equal(0, row.LunchHours);
            Assert.Null(row.TotalDurationHours);
        });
    }

    [Fact]
    public async Task GetDailyBreakdownAsync_WithSessions_CalculatesCorrectBreakdownPerDay()
    {
        // Arrange
        const long userId = 123456;
        var startDate = DateTime.UtcNow.Date;
        var endDate = startDate.AddDays(3);
        var day1 = startDate;
        var day2 = startDate.AddDays(1);

        var sessions = new List<TrackingSession>
        {
            // Day 1: Full work day
            new(userId, TrackingState.Commuting, day1.AddHours(8), CommuteDirection.ToWork),
            new(userId, TrackingState.Working, day1.AddHours(9)),
            new(userId, TrackingState.Lunch, day1.AddHours(12)),
            new(userId, TrackingState.Working, day1.AddHours(13)),
            new(userId, TrackingState.Commuting, day1.AddHours(17), CommuteDirection.ToHome),

            // Day 2: Partial work day
            new(userId, TrackingState.Working, day2.AddHours(9)),
        };

        sessions[0].End(day1.AddHours(9));   // 1 hour commute to work
        sessions[1].End(day1.AddHours(12));  // 3 hours work
        sessions[2].End(day1.AddHours(13));  // 1 hour lunch
        sessions[3].End(day1.AddHours(17));  // 4 hours work
        sessions[4].End(day1.AddHours(17.5)); // 0.5 hours commute to home
        sessions[5].End(day2.AddHours(15));  // 6 hours work

        _mockTrackingSessionRepository
            .Setup(x => x.GetSessionsInRangeAsync(userId, startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        // Act
        var result = await _sut.GetDailyBreakdownAsync(userId, startDate, endDate, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Count);

        // Day 1
        var day1Row = result[0];
        Assert.Equal(day1, day1Row.Date);
        Assert.True(day1Row.HasActivity);
        Assert.Equal(7m, day1Row.WorkHours); // 3 + 4
        Assert.Equal(1m, day1Row.CommuteToWorkHours);
        Assert.Equal(0.5m, day1Row.CommuteToHomeHours);
        Assert.Equal(1m, day1Row.LunchHours);
        Assert.Equal(9.5m, day1Row.TotalDurationHours); // 8:00 to 17:30

        // Day 2
        var day2Row = result[1];
        Assert.Equal(day2, day2Row.Date);
        Assert.True(day2Row.HasActivity);
        Assert.Equal(6m, day2Row.WorkHours);
        Assert.Equal(0m, day2Row.CommuteToWorkHours);
        Assert.Equal(0m, day2Row.CommuteToHomeHours);
        Assert.Equal(0m, day2Row.LunchHours);
        Assert.Equal(6m, day2Row.TotalDurationHours); // 9:00 to 15:00

        // Day 3 (no activity)
        var day3Row = result[2];
        Assert.False(day3Row.HasActivity);
        Assert.Equal(0m, day3Row.WorkHours);
        Assert.Null(day3Row.TotalDurationHours);
    }

    [Fact]
    public async Task GetDailyBreakdownAsync_IncludesAllDaysInRange()
    {
        // Arrange
        const long userId = 123456;
        var startDate = new DateTime(2026, 2, 1); // Feb 1
        var endDate = new DateTime(2026, 2, 8);   // Feb 8 (7 days)

        // Only one session on Feb 3
        var sessions = new List<TrackingSession>
        {
            new(userId, TrackingState.Working, new DateTime(2026, 2, 3, 9, 0, 0))
        };
        sessions[0].End(new DateTime(2026, 2, 3, 17, 0, 0));

        _mockTrackingSessionRepository
            .Setup(x => x.GetSessionsInRangeAsync(userId, startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        // Act
        var result = await _sut.GetDailyBreakdownAsync(userId, startDate, endDate, CancellationToken.None);

        // Assert
        Assert.Equal(7, result.Count);

        // Check that only Feb 3 has activity
        Assert.False(result[0].HasActivity); // Feb 1
        Assert.False(result[1].HasActivity); // Feb 2
        Assert.True(result[2].HasActivity);  // Feb 3
        Assert.False(result[3].HasActivity); // Feb 4
        Assert.False(result[4].HasActivity); // Feb 5
        Assert.False(result[5].HasActivity); // Feb 6
        Assert.False(result[6].HasActivity); // Feb 7

        Assert.Equal(8m, result[2].WorkHours); // 9:00 to 17:00
    }

    [Fact]
    public async Task GetDailyBreakdownAsync_HandlesActiveSessions()
    {
        // Arrange
        const long userId = 123456;
        var startDate = DateTime.UtcNow.Date;
        var endDate = startDate.AddDays(1);

        var sessions = new List<TrackingSession>
        {
            new(userId, TrackingState.Working, startDate.AddHours(9)),
            // Active session (no end time)
            new(userId, TrackingState.Working, startDate.AddHours(14)),
        };

        sessions[0].End(startDate.AddHours(12)); // 3 hours work

        _mockTrackingSessionRepository
            .Setup(x => x.GetSessionsInRangeAsync(userId, startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        // Act
        var result = await _sut.GetDailyBreakdownAsync(userId, startDate, endDate, CancellationToken.None);

        // Assert
        Assert.Single(result);
        var row = result[0];
        Assert.True(row.HasActivity);
        Assert.Equal(3m, row.WorkHours); // Only completed session counts
        Assert.NotNull(row.TotalDurationHours);
        // Total duration should span from 9:00 to now (active session)
        Assert.True(row.TotalDurationHours >= 5m); // At least 5 hours (9:00 to 14:00)
    }

    [Fact]
    public async Task GetDailyBreakdownAsync_SeparatesCommuteDirections()
    {
        // Arrange
        const long userId = 123456;
        var startDate = DateTime.UtcNow.Date;
        var endDate = startDate.AddDays(1);

        var sessions = new List<TrackingSession>
        {
            new(userId, TrackingState.Commuting, startDate.AddHours(8), CommuteDirection.ToWork),
            new(userId, TrackingState.Working, startDate.AddHours(9)),
            new(userId, TrackingState.Commuting, startDate.AddHours(17), CommuteDirection.ToHome),
        };

        sessions[0].End(startDate.AddHours(9));   // 1 hour to work
        sessions[1].End(startDate.AddHours(17));  // 8 hours work
        sessions[2].End(startDate.AddHours(17.75)); // 0.75 hours to home

        _mockTrackingSessionRepository
            .Setup(x => x.GetSessionsInRangeAsync(userId, startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        // Act
        var result = await _sut.GetDailyBreakdownAsync(userId, startDate, endDate, CancellationToken.None);

        // Assert
        var row = result[0];
        Assert.Equal(1m, row.CommuteToWorkHours);
        Assert.Equal(0.75m, row.CommuteToHomeHours);
        Assert.Equal(8m, row.WorkHours);
    }
}
