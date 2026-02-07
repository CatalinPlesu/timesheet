namespace TimeSheet.Tests.Unit.Entities;

using TimeSheet.Core.Domain.Entities;

public class UserTests
{
    [Fact]
    public void UpdateWorkLimit_WithValidValue_ShouldUpdateMaxWorkHours()
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);

        // Act
        user.UpdateWorkLimit(8.5m);

        // Assert
        Assert.Equal(8.5m, user.MaxWorkHours);
    }

    [Fact]
    public void UpdateWorkLimit_WithNull_ShouldSetNoLimit()
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);
        user.UpdateWorkLimit(8.0m);

        // Act
        user.UpdateWorkLimit(null);

        // Assert
        Assert.Null(user.MaxWorkHours);
    }

    [Fact]
    public void UpdateWorkLimit_WithZero_ShouldThrowArgumentException()
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => user.UpdateWorkLimit(0m));
        Assert.Contains("positive", exception.Message);
    }

    [Fact]
    public void UpdateWorkLimit_WithNegative_ShouldThrowArgumentException()
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => user.UpdateWorkLimit(-1m));
        Assert.Contains("positive", exception.Message);
    }

    [Fact]
    public void UpdateCommuteLimit_WithValidValue_ShouldUpdateMaxCommuteHours()
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);

        // Act
        user.UpdateCommuteLimit(2.0m);

        // Assert
        Assert.Equal(2.0m, user.MaxCommuteHours);
    }

    [Fact]
    public void UpdateCommuteLimit_WithNull_ShouldSetNoLimit()
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);
        user.UpdateCommuteLimit(1.5m);

        // Act
        user.UpdateCommuteLimit(null);

        // Assert
        Assert.Null(user.MaxCommuteHours);
    }

    [Fact]
    public void UpdateCommuteLimit_WithZero_ShouldThrowArgumentException()
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => user.UpdateCommuteLimit(0m));
        Assert.Contains("positive", exception.Message);
    }

    [Fact]
    public void UpdateLunchLimit_WithValidValue_ShouldUpdateMaxLunchHours()
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);

        // Act
        user.UpdateLunchLimit(1.5m);

        // Assert
        Assert.Equal(1.5m, user.MaxLunchHours);
    }

    [Fact]
    public void UpdateLunchLimit_WithNull_ShouldSetNoLimit()
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);
        user.UpdateLunchLimit(1.0m);

        // Act
        user.UpdateLunchLimit(null);

        // Assert
        Assert.Null(user.MaxLunchHours);
    }

    [Fact]
    public void UpdateLunchLimit_WithZero_ShouldThrowArgumentException()
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => user.UpdateLunchLimit(0m));
        Assert.Contains("positive", exception.Message);
    }

    [Fact]
    public void Constructor_ShouldInitializeWithNoAutoShutdownLimits()
    {
        // Act
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);

        // Assert
        Assert.Null(user.MaxWorkHours);
        Assert.Null(user.MaxCommuteHours);
        Assert.Null(user.MaxLunchHours);
    }

    [Fact]
    public void RehydrationConstructor_ShouldPreserveAutoShutdownLimits()
    {
        // Arrange
        var id = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow.AddDays(-1);
        var registeredAt = DateTimeOffset.UtcNow.AddDays(-1);

        // Act
        var user = new User(
            id,
            createdAt,
            123456789,
            "testuser",
            isAdmin: true,
            utcOffsetMinutes: 0,
            registeredAt,
            maxWorkHours: 8.5m,
            maxCommuteHours: 2.0m,
            maxLunchHours: 1.5m);

        // Assert
        Assert.Equal(8.5m, user.MaxWorkHours);
        Assert.Equal(2.0m, user.MaxCommuteHours);
        Assert.Equal(1.5m, user.MaxLunchHours);
    }

    [Fact]
    public void UpdateLunchReminderHour_WithValidValue_ShouldUpdateLunchReminderHour()
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);

        // Act
        user.UpdateLunchReminderHour(12);

        // Assert
        Assert.Equal(12, user.LunchReminderHour);
    }

    [Fact]
    public void UpdateLunchReminderHour_WithNull_ShouldDisableReminder()
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);
        user.UpdateLunchReminderHour(12);

        // Act
        user.UpdateLunchReminderHour(null);

        // Assert
        Assert.Null(user.LunchReminderHour);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(12)]
    [InlineData(23)]
    public void UpdateLunchReminderHour_WithValidHours_ShouldUpdateSuccessfully(int hour)
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);

        // Act
        user.UpdateLunchReminderHour(hour);

        // Assert
        Assert.Equal(hour, user.LunchReminderHour);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(24)]
    [InlineData(25)]
    [InlineData(100)]
    public void UpdateLunchReminderHour_WithInvalidHours_ShouldThrowArgumentException(int hour)
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => user.UpdateLunchReminderHour(hour));
        Assert.Contains("between 0 and 23", exception.Message);
    }

    [Fact]
    public void Constructor_ShouldInitializeWithNoLunchReminder()
    {
        // Act
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);

        // Assert
        Assert.Null(user.LunchReminderHour);
    }

    [Fact]
    public void RehydrationConstructor_ShouldPreserveLunchReminderHour()
    {
        // Arrange
        var id = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow.AddDays(-1);
        var registeredAt = DateTimeOffset.UtcNow.AddDays(-1);

        // Act
        var user = new User(
            id,
            createdAt,
            123456789,
            "testuser",
            isAdmin: true,
            utcOffsetMinutes: 0,
            registeredAt,
            maxWorkHours: 8.5m,
            maxCommuteHours: 2.0m,
            maxLunchHours: 1.5m,
            lunchReminderHour: 12);

        // Assert
        Assert.Equal(12, user.LunchReminderHour);
    }

    [Fact]
    public void UpdateTargetWorkHours_WithValidValue_ShouldUpdateTargetWorkHours()
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);

        // Act
        user.UpdateTargetWorkHours(8.0m);

        // Assert
        Assert.Equal(8.0m, user.TargetWorkHours);
    }

    [Fact]
    public void UpdateTargetWorkHours_WithNull_ShouldDisableTarget()
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);
        user.UpdateTargetWorkHours(8.0m);

        // Act
        user.UpdateTargetWorkHours(null);

        // Assert
        Assert.Null(user.TargetWorkHours);
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(7.5)]
    [InlineData(8.0)]
    [InlineData(10.5)]
    public void UpdateTargetWorkHours_WithValidHours_ShouldUpdateSuccessfully(decimal hours)
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);

        // Act
        user.UpdateTargetWorkHours(hours);

        // Assert
        Assert.Equal(hours, user.TargetWorkHours);
    }

    [Fact]
    public void UpdateTargetWorkHours_WithZero_ShouldThrowArgumentException()
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => user.UpdateTargetWorkHours(0m));
        Assert.Contains("positive", exception.Message);
    }

    [Fact]
    public void UpdateTargetWorkHours_WithNegative_ShouldThrowArgumentException()
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => user.UpdateTargetWorkHours(-1m));
        Assert.Contains("positive", exception.Message);
    }

    [Fact]
    public void Constructor_ShouldInitializeWithNoTargetWorkHours()
    {
        // Act
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);

        // Assert
        Assert.Null(user.TargetWorkHours);
    }

    [Fact]
    public void RehydrationConstructor_ShouldPreserveTargetWorkHours()
    {
        // Arrange
        var id = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow.AddDays(-1);
        var registeredAt = DateTimeOffset.UtcNow.AddDays(-1);

        // Act
        var user = new User(
            id,
            createdAt,
            123456789,
            "testuser",
            isAdmin: true,
            utcOffsetMinutes: 0,
            registeredAt,
            maxWorkHours: 8.5m,
            maxCommuteHours: 2.0m,
            maxLunchHours: 1.5m,
            lunchReminderHour: 12,
            targetWorkHours: 8.0m);

        // Assert
        Assert.Equal(8.0m, user.TargetWorkHours);
    }

    [Fact]
    public void UpdateForgotShutdownThreshold_WithValidValue_ShouldUpdateThreshold()
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);

        // Act
        user.UpdateForgotShutdownThreshold(150);

        // Assert
        Assert.Equal(150, user.ForgotShutdownThresholdPercent);
    }

    [Fact]
    public void UpdateForgotShutdownThreshold_WithNull_ShouldDisableDetection()
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);
        user.UpdateForgotShutdownThreshold(150);

        // Act
        user.UpdateForgotShutdownThreshold(null);

        // Assert
        Assert.Null(user.ForgotShutdownThresholdPercent);
    }

    [Theory]
    [InlineData(101)]
    [InlineData(150)]
    [InlineData(200)]
    [InlineData(300)]
    public void UpdateForgotShutdownThreshold_WithValidPercent_ShouldUpdateSuccessfully(int percent)
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);

        // Act
        user.UpdateForgotShutdownThreshold(percent);

        // Assert
        Assert.Equal(percent, user.ForgotShutdownThresholdPercent);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    public void UpdateForgotShutdownThreshold_WithInvalidPercent_ShouldThrowArgumentException(int percent)
    {
        // Arrange
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => user.UpdateForgotShutdownThreshold(percent));
        Assert.Contains("greater than 100", exception.Message);
    }

    [Fact]
    public void Constructor_ShouldInitializeWithNoForgotShutdownThreshold()
    {
        // Act
        var user = new User(123456789, "testuser", isAdmin: true, utcOffsetMinutes: 0);

        // Assert
        Assert.Null(user.ForgotShutdownThresholdPercent);
    }

    [Fact]
    public void RehydrationConstructor_ShouldPreserveForgotShutdownThreshold()
    {
        // Arrange
        var id = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow.AddDays(-1);
        var registeredAt = DateTimeOffset.UtcNow.AddDays(-1);

        // Act
        var user = new User(
            id,
            createdAt,
            123456789,
            "testuser",
            isAdmin: true,
            utcOffsetMinutes: 0,
            registeredAt,
            maxWorkHours: 8.5m,
            maxCommuteHours: 2.0m,
            maxLunchHours: 1.5m,
            lunchReminderHour: 12,
            targetWorkHours: 8.0m,
            forgotShutdownThresholdPercent: 150);

        // Assert
        Assert.Equal(150, user.ForgotShutdownThresholdPercent);
    }
}
