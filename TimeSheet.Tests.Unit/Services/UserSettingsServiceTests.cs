namespace TimeSheet.Tests.Unit.Services;

using Moq;
using TimeSheet.Core.Application.Interfaces;
using TimeSheet.Core.Application.Services;
using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.Enums;

public class UserSettingsServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly UserSettingsService _service;

    public UserSettingsServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _service = new UserSettingsService(_mockUserRepository.Object, _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task UpdateAutoShutdownLimitAsync_ForWorkState_ShouldUpdateWorkLimit()
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);
        _mockUserRepository
            .Setup(r => r.GetByTelegramUserIdAsync(123456789, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.UpdateAutoShutdownLimitAsync(123456789, TrackingState.Working, 8.5m);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(8.5m, result.MaxWorkHours);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAutoShutdownLimitAsync_ForCommuteState_ShouldUpdateCommuteLimit()
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);
        _mockUserRepository
            .Setup(r => r.GetByTelegramUserIdAsync(123456789, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.UpdateAutoShutdownLimitAsync(123456789, TrackingState.Commuting, 2.0m);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2.0m, result.MaxCommuteHours);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAutoShutdownLimitAsync_ForLunchState_ShouldUpdateLunchLimit()
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);
        _mockUserRepository
            .Setup(r => r.GetByTelegramUserIdAsync(123456789, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.UpdateAutoShutdownLimitAsync(123456789, TrackingState.Lunch, 1.5m);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1.5m, result.MaxLunchHours);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAutoShutdownLimitAsync_ForIdleState_ShouldThrowArgumentException()
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);
        _mockUserRepository
            .Setup(r => r.GetByTelegramUserIdAsync(123456789, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _service.UpdateAutoShutdownLimitAsync(123456789, TrackingState.Idle, 1.0m));
    }

    [Fact]
    public async Task UpdateAutoShutdownLimitAsync_WithNullLimit_ShouldRemoveLimit()
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);
        user.UpdateWorkLimit(8.0m);
        _mockUserRepository
            .Setup(r => r.GetByTelegramUserIdAsync(123456789, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.UpdateAutoShutdownLimitAsync(123456789, TrackingState.Working, null);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.MaxWorkHours);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAutoShutdownLimitAsync_WithNonExistentUser_ShouldReturnNull()
    {
        // Arrange
        _mockUserRepository
            .Setup(r => r.GetByTelegramUserIdAsync(123456789, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _service.UpdateAutoShutdownLimitAsync(123456789, TrackingState.Working, 8.5m);

        // Assert
        Assert.Null(result);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateLunchReminderHourAsync_WithValidHour_ShouldUpdateLunchReminderHour()
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);
        _mockUserRepository
            .Setup(r => r.GetByTelegramUserIdAsync(123456789, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.UpdateLunchReminderHourAsync(123456789, 12);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(12, result.LunchReminderHour);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateLunchReminderHourAsync_WithNull_ShouldDisableReminder()
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);
        user.UpdateLunchReminderHour(12);
        _mockUserRepository
            .Setup(r => r.GetByTelegramUserIdAsync(123456789, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.UpdateLunchReminderHourAsync(123456789, null);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.LunchReminderHour);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(12)]
    [InlineData(23)]
    public async Task UpdateLunchReminderHourAsync_WithValidHours_ShouldUpdateSuccessfully(int hour)
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);
        _mockUserRepository
            .Setup(r => r.GetByTelegramUserIdAsync(123456789, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.UpdateLunchReminderHourAsync(123456789, hour);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(hour, result.LunchReminderHour);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateLunchReminderHourAsync_WithNonExistentUser_ShouldReturnNull()
    {
        // Arrange
        _mockUserRepository
            .Setup(r => r.GetByTelegramUserIdAsync(123456789, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _service.UpdateLunchReminderHourAsync(123456789, 12);

        // Assert
        Assert.Null(result);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateTargetWorkHoursAsync_WithValidHours_ShouldUpdateTargetWorkHours()
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);
        _mockUserRepository
            .Setup(r => r.GetByTelegramUserIdAsync(123456789, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.UpdateTargetWorkHoursAsync(123456789, 8.0m);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(8.0m, result.TargetWorkHours);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTargetWorkHoursAsync_WithNull_ShouldDisableTarget()
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);
        user.UpdateTargetWorkHours(8.0m);
        _mockUserRepository
            .Setup(r => r.GetByTelegramUserIdAsync(123456789, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.UpdateTargetWorkHoursAsync(123456789, null);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.TargetWorkHours);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(7.5)]
    [InlineData(8.0)]
    [InlineData(10.5)]
    public async Task UpdateTargetWorkHoursAsync_WithValidValues_ShouldUpdateSuccessfully(decimal hours)
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);
        _mockUserRepository
            .Setup(r => r.GetByTelegramUserIdAsync(123456789, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.UpdateTargetWorkHoursAsync(123456789, hours);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(hours, result.TargetWorkHours);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTargetWorkHoursAsync_WithNonExistentUser_ShouldReturnNull()
    {
        // Arrange
        _mockUserRepository
            .Setup(r => r.GetByTelegramUserIdAsync(123456789, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _service.UpdateTargetWorkHoursAsync(123456789, 8.0m);

        // Assert
        Assert.Null(result);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
