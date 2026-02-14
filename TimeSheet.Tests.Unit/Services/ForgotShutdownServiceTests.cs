namespace TimeSheet.Tests.Unit.Services;

using Microsoft.Extensions.Logging;
using Moq;
using TimeSheet.Core.Application.Interfaces.Services;
using TimeSheet.Core.Application.Interfaces.Persistence;
using TimeSheet.Core.Domain.Repositories;
using TimeSheet.Core.Application.Services;
using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.Enums;

public class ForgotShutdownServiceTests
{
    private readonly Mock<ITrackingSessionRepository> _mockTrackingSessionRepository;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<ILogger<ForgotShutdownService>> _mockLogger;
    private readonly ForgotShutdownService _service;

    public ForgotShutdownServiceTests()
    {
        _mockTrackingSessionRepository = new Mock<ITrackingSessionRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockLogger = new Mock<ILogger<ForgotShutdownService>>();
        _service = new ForgotShutdownService(
            _mockTrackingSessionRepository.Object,
            _mockUserRepository.Object,
            _mockNotificationService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task CheckAndNotifyLongRunningSessionsAsync_WithNoActiveSessions_ShouldReturnEmptyList()
    {
        // Arrange
        _mockTrackingSessionRepository
            .Setup(r => r.GetAllActiveSessionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await _service.CheckAndNotifyLongRunningSessionsAsync();

        // Assert
        Assert.Empty(result);
        _mockNotificationService.Verify(
            n => n.SendForgotShutdownReminderAsync(
                It.IsAny<long>(),
                It.IsAny<TrackingState>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CheckAndNotifyLongRunningSessionsAsync_WithSessionBelowThreshold_ShouldNotNotify()
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);
        user.UpdateForgotShutdownThreshold(150); // 150% threshold

        var session = new TrackingSession(
            123456789,
            TrackingState.Working,
            DateTime.UtcNow.AddHours(-4)); // 4 hours

        _mockTrackingSessionRepository
            .Setup(r => r.GetAllActiveSessionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([session]);

        _mockUserRepository
            .Setup(r => r.GetByTelegramUserIdAsync(123456789, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Average is 8 hours, threshold is 150% = 12 hours, current is 4 hours (below)
        _mockTrackingSessionRepository
            .Setup(r => r.GetAverageDurationAsync(123456789, TrackingState.Working, It.IsAny<CancellationToken>()))
            .ReturnsAsync(8.0m);

        // Act
        var result = await _service.CheckAndNotifyLongRunningSessionsAsync();

        // Assert
        Assert.Empty(result);
        _mockNotificationService.Verify(
            n => n.SendForgotShutdownReminderAsync(
                It.IsAny<long>(),
                It.IsAny<TrackingState>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CheckAndNotifyLongRunningSessionsAsync_WithSessionExceedingThreshold_ShouldNotify()
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);
        user.UpdateForgotShutdownThreshold(150); // 150% threshold

        var session = new TrackingSession(
            123456789,
            TrackingState.Working,
            DateTime.UtcNow.AddHours(-13)); // 13 hours

        _mockTrackingSessionRepository
            .Setup(r => r.GetAllActiveSessionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([session]);

        _mockUserRepository
            .Setup(r => r.GetByTelegramUserIdAsync(123456789, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Average is 8 hours, threshold is 150% = 12 hours, current is 13 hours (exceeds)
        _mockTrackingSessionRepository
            .Setup(r => r.GetAverageDurationAsync(123456789, TrackingState.Working, It.IsAny<CancellationToken>()))
            .ReturnsAsync(8.0m);

        // Act
        var result = await _service.CheckAndNotifyLongRunningSessionsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal(session, result[0]);
        _mockNotificationService.Verify(
            n => n.SendForgotShutdownReminderAsync(
                123456789,
                TrackingState.Working,
                It.IsInRange(12.9m, 13.1m, Moq.Range.Inclusive), // Allow small variance
                8.0m,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckAndNotifyLongRunningSessionsAsync_WithNoThresholdConfigured_ShouldNotNotify()
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);
        // No threshold configured

        var session = new TrackingSession(
            123456789,
            TrackingState.Working,
            DateTime.UtcNow.AddHours(-20)); // Very long session

        _mockTrackingSessionRepository
            .Setup(r => r.GetAllActiveSessionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([session]);

        _mockUserRepository
            .Setup(r => r.GetByTelegramUserIdAsync(123456789, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.CheckAndNotifyLongRunningSessionsAsync();

        // Assert
        Assert.Empty(result);
        _mockNotificationService.Verify(
            n => n.SendForgotShutdownReminderAsync(
                It.IsAny<long>(),
                It.IsAny<TrackingState>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CheckAndNotifyLongRunningSessionsAsync_WithNoHistoricalData_ShouldNotNotify()
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);
        user.UpdateForgotShutdownThreshold(150);

        var session = new TrackingSession(
            123456789,
            TrackingState.Working,
            DateTime.UtcNow.AddHours(-10));

        _mockTrackingSessionRepository
            .Setup(r => r.GetAllActiveSessionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([session]);

        _mockUserRepository
            .Setup(r => r.GetByTelegramUserIdAsync(123456789, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // No historical data available
        _mockTrackingSessionRepository
            .Setup(r => r.GetAverageDurationAsync(123456789, TrackingState.Working, It.IsAny<CancellationToken>()))
            .ReturnsAsync((decimal?)null);

        // Act
        var result = await _service.CheckAndNotifyLongRunningSessionsAsync();

        // Assert
        Assert.Empty(result);
        _mockNotificationService.Verify(
            n => n.SendForgotShutdownReminderAsync(
                It.IsAny<long>(),
                It.IsAny<TrackingState>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CheckAndNotifyLongRunningSessionsAsync_WithUserNotFound_ShouldSkipSession()
    {
        // Arrange
        var session = new TrackingSession(
            123456789,
            TrackingState.Working,
            DateTime.UtcNow.AddHours(-10));

        _mockTrackingSessionRepository
            .Setup(r => r.GetAllActiveSessionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([session]);

        _mockUserRepository
            .Setup(r => r.GetByTelegramUserIdAsync(123456789, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _service.CheckAndNotifyLongRunningSessionsAsync();

        // Assert
        Assert.Empty(result);
        _mockNotificationService.Verify(
            n => n.SendForgotShutdownReminderAsync(
                It.IsAny<long>(),
                It.IsAny<TrackingState>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CheckAndNotifyLongRunningSessionsAsync_WithCommuteSession_ShouldCheckCommuteAverage()
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);
        user.UpdateForgotShutdownThreshold(200); // 200% threshold

        var session = new TrackingSession(
            123456789,
            TrackingState.Commuting,
            DateTime.UtcNow.AddHours(-2.5), // 2.5 hours
            CommuteDirection.ToWork);

        _mockTrackingSessionRepository
            .Setup(r => r.GetAllActiveSessionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([session]);

        _mockUserRepository
            .Setup(r => r.GetByTelegramUserIdAsync(123456789, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Average is 1 hour, threshold is 200% = 2 hours, current is 2.5 hours (exceeds)
        _mockTrackingSessionRepository
            .Setup(r => r.GetAverageDurationAsync(123456789, TrackingState.Commuting, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1.0m);

        // Act
        var result = await _service.CheckAndNotifyLongRunningSessionsAsync();

        // Assert
        Assert.Single(result);
        _mockNotificationService.Verify(
            n => n.SendForgotShutdownReminderAsync(
                123456789,
                TrackingState.Commuting,
                It.IsInRange(2.4m, 2.6m, Moq.Range.Inclusive),
                1.0m,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckAndNotifyLongRunningSessionsAsync_WithLunchSession_ShouldCheckLunchAverage()
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);
        user.UpdateForgotShutdownThreshold(150); // 150% threshold

        var session = new TrackingSession(
            123456789,
            TrackingState.Lunch,
            DateTime.UtcNow.AddHours(-1.6)); // 1.6 hours

        _mockTrackingSessionRepository
            .Setup(r => r.GetAllActiveSessionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([session]);

        _mockUserRepository
            .Setup(r => r.GetByTelegramUserIdAsync(123456789, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Average is 1 hour, threshold is 150% = 1.5 hours, current is 1.6 hours (exceeds)
        _mockTrackingSessionRepository
            .Setup(r => r.GetAverageDurationAsync(123456789, TrackingState.Lunch, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1.0m);

        // Act
        var result = await _service.CheckAndNotifyLongRunningSessionsAsync();

        // Assert
        Assert.Single(result);
        _mockNotificationService.Verify(
            n => n.SendForgotShutdownReminderAsync(
                123456789,
                TrackingState.Lunch,
                It.IsInRange(1.5m, 1.7m, Moq.Range.Inclusive),
                1.0m,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ShouldNotifyForSessionAsync_WithInactiveSession_ShouldReturnFalse()
    {
        // Arrange
        var session = new TrackingSession(
            Guid.NewGuid(),
            123456789,
            TrackingState.Working,
            DateTime.UtcNow.AddHours(-10),
            DateTime.UtcNow.AddHours(-2)); // Session already ended

        // Act
        var result = await _service.ShouldNotifyForSessionAsync(session);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ShouldNotifyForSessionAsync_WithNoThreshold_ShouldReturnFalse()
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);
        // No threshold configured

        var session = new TrackingSession(
            123456789,
            TrackingState.Working,
            DateTime.UtcNow.AddHours(-10));

        _mockUserRepository
            .Setup(r => r.GetByTelegramUserIdAsync(123456789, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.ShouldNotifyForSessionAsync(session);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ShouldNotifyForSessionAsync_WithNoHistoricalData_ShouldReturnFalse()
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);
        user.UpdateForgotShutdownThreshold(150);

        var session = new TrackingSession(
            123456789,
            TrackingState.Working,
            DateTime.UtcNow.AddHours(-10));

        _mockUserRepository
            .Setup(r => r.GetByTelegramUserIdAsync(123456789, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockTrackingSessionRepository
            .Setup(r => r.GetAverageDurationAsync(123456789, TrackingState.Working, It.IsAny<CancellationToken>()))
            .ReturnsAsync((decimal?)null);

        // Act
        var result = await _service.ShouldNotifyForSessionAsync(session);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ShouldNotifyForSessionAsync_WithSessionBelowThreshold_ShouldReturnFalse()
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);
        user.UpdateForgotShutdownThreshold(150);

        var session = new TrackingSession(
            123456789,
            TrackingState.Working,
            DateTime.UtcNow.AddHours(-4)); // 4 hours

        _mockUserRepository
            .Setup(r => r.GetByTelegramUserIdAsync(123456789, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Average is 8 hours, threshold is 150% = 12 hours, current is 4 hours (below)
        _mockTrackingSessionRepository
            .Setup(r => r.GetAverageDurationAsync(123456789, TrackingState.Working, It.IsAny<CancellationToken>()))
            .ReturnsAsync(8.0m);

        // Act
        var result = await _service.ShouldNotifyForSessionAsync(session);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ShouldNotifyForSessionAsync_WithSessionExceedingThreshold_ShouldReturnTrue()
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);
        user.UpdateForgotShutdownThreshold(150);

        var session = new TrackingSession(
            123456789,
            TrackingState.Working,
            DateTime.UtcNow.AddHours(-13)); // 13 hours

        _mockUserRepository
            .Setup(r => r.GetByTelegramUserIdAsync(123456789, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Average is 8 hours, threshold is 150% = 12 hours, current is 13 hours (exceeds)
        _mockTrackingSessionRepository
            .Setup(r => r.GetAverageDurationAsync(123456789, TrackingState.Working, It.IsAny<CancellationToken>()))
            .ReturnsAsync(8.0m);

        // Act
        var result = await _service.ShouldNotifyForSessionAsync(session);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ShouldNotifyForSessionAsync_WithSessionExactlyAtThreshold_ShouldReturnTrue()
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);
        user.UpdateForgotShutdownThreshold(150);

        var session = new TrackingSession(
            123456789,
            TrackingState.Working,
            DateTime.UtcNow.AddHours(-12)); // Exactly 12 hours

        _mockUserRepository
            .Setup(r => r.GetByTelegramUserIdAsync(123456789, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Average is 8 hours, threshold is 150% = 12 hours, current is 12 hours (equals)
        _mockTrackingSessionRepository
            .Setup(r => r.GetAverageDurationAsync(123456789, TrackingState.Working, It.IsAny<CancellationToken>()))
            .ReturnsAsync(8.0m);

        // Act
        var result = await _service.ShouldNotifyForSessionAsync(session);

        // Assert
        Assert.True(result);
    }
}
