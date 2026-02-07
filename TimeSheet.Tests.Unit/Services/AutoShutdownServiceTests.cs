namespace TimeSheet.Tests.Unit.Services;

using Moq;
using TimeSheet.Core.Application.Interfaces;
using TimeSheet.Core.Application.Services;
using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.Enums;

public class AutoShutdownServiceTests
{
    private readonly Mock<ITrackingSessionRepository> _mockTrackingSessionRepository;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly AutoShutdownService _service;

    public AutoShutdownServiceTests()
    {
        _mockTrackingSessionRepository = new Mock<ITrackingSessionRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _service = new AutoShutdownService(
            _mockTrackingSessionRepository.Object,
            _mockUserRepository.Object,
            _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task CheckAndShutdownLongRunningSessionsAsync_WithNoActiveSessions_ShouldReturnEmptyList()
    {
        // Arrange
        _mockTrackingSessionRepository
            .Setup(r => r.GetAllActiveSessionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await _service.CheckAndShutdownLongRunningSessionsAsync();

        // Assert
        Assert.Empty(result);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckAndShutdownLongRunningSessionsAsync_WithSessionBelowLimit_ShouldNotShutdown()
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);
        user.UpdateWorkLimit(8.0m);

        var session = new TrackingSession(
            123456789,
            TrackingState.Working,
            DateTime.UtcNow.AddHours(-4)); // 4 hours ago, below 8-hour limit

        _mockTrackingSessionRepository
            .Setup(r => r.GetAllActiveSessionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([session]);

        _mockUserRepository
            .Setup(r => r.GetByTelegramUserIdAsync(123456789, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.CheckAndShutdownLongRunningSessionsAsync();

        // Assert
        Assert.Empty(result);
        Assert.True(session.IsActive);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckAndShutdownLongRunningSessionsAsync_WithSessionExceedingLimit_ShouldShutdown()
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);
        user.UpdateWorkLimit(8.0m);

        var session = new TrackingSession(
            123456789,
            TrackingState.Working,
            DateTime.UtcNow.AddHours(-9)); // 9 hours ago, exceeds 8-hour limit

        _mockTrackingSessionRepository
            .Setup(r => r.GetAllActiveSessionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([session]);

        _mockUserRepository
            .Setup(r => r.GetByTelegramUserIdAsync(123456789, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.CheckAndShutdownLongRunningSessionsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal(session, result[0]);
        Assert.False(session.IsActive);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckAndShutdownLongRunningSessionsAsync_WithNoLimit_ShouldNotShutdown()
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);
        // No limit configured

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
        var result = await _service.CheckAndShutdownLongRunningSessionsAsync();

        // Assert
        Assert.Empty(result);
        Assert.True(session.IsActive);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckAndShutdownLongRunningSessionsAsync_WithMultipleSessions_ShouldShutdownOnlyExceeding()
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);
        user.UpdateWorkLimit(8.0m);
        user.UpdateCommuteLimit(2.0m);

        var workSession = new TrackingSession(
            123456789,
            TrackingState.Working,
            DateTime.UtcNow.AddHours(-9)); // Exceeds limit

        var commuteSession = new TrackingSession(
            123456789,
            TrackingState.Commuting,
            DateTime.UtcNow.AddMinutes(-30),
            CommuteDirection.ToWork); // Below limit

        _mockTrackingSessionRepository
            .Setup(r => r.GetAllActiveSessionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([workSession, commuteSession]);

        _mockUserRepository
            .Setup(r => r.GetByTelegramUserIdAsync(123456789, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.CheckAndShutdownLongRunningSessionsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal(workSession, result[0]);
        Assert.False(workSession.IsActive);
        Assert.True(commuteSession.IsActive);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckAndShutdownLongRunningSessionsAsync_WithNonExistentUser_ShouldSkipSession()
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
        var result = await _service.CheckAndShutdownLongRunningSessionsAsync();

        // Assert
        Assert.Empty(result);
        Assert.True(session.IsActive);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckAndShutdownLongRunningSessionsAsync_WithCommuteSession_ShouldRespectCommuteLimit()
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);
        user.UpdateCommuteLimit(1.5m);

        var session = new TrackingSession(
            123456789,
            TrackingState.Commuting,
            DateTime.UtcNow.AddHours(-2),
            CommuteDirection.ToWork); // Exceeds 1.5 hour limit

        _mockTrackingSessionRepository
            .Setup(r => r.GetAllActiveSessionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([session]);

        _mockUserRepository
            .Setup(r => r.GetByTelegramUserIdAsync(123456789, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.CheckAndShutdownLongRunningSessionsAsync();

        // Assert
        Assert.Single(result);
        Assert.False(session.IsActive);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CheckAndShutdownLongRunningSessionsAsync_WithLunchSession_ShouldRespectLunchLimit()
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);
        user.UpdateLunchLimit(1.0m);

        var session = new TrackingSession(
            123456789,
            TrackingState.Lunch,
            DateTime.UtcNow.AddHours(-1.5)); // Exceeds 1 hour limit

        _mockTrackingSessionRepository
            .Setup(r => r.GetAllActiveSessionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([session]);

        _mockUserRepository
            .Setup(r => r.GetByTelegramUserIdAsync(123456789, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.CheckAndShutdownLongRunningSessionsAsync();

        // Assert
        Assert.Single(result);
        Assert.False(session.IsActive);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
