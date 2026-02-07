using Moq;
using TimeSheet.Core.Application.Interfaces;
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
}
