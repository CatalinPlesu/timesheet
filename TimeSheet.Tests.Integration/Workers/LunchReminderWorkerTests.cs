namespace TimeSheet.Tests.Integration.Workers;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TimeSheet.Core.Application.Interfaces.Services;
using TimeSheet.Core.Application.Interfaces.Persistence;
using TimeSheet.Core.Domain.Repositories;
using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.Enums;
using TimeSheet.Presentation.Telegram.Options;
using TimeSheet.Presentation.Telegram.Workers;

public class LunchReminderWorkerTests
{
    private readonly Mock<IServiceScopeFactory> _mockServiceScopeFactory;
    private readonly Mock<IServiceScope> _mockServiceScope;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ITrackingSessionRepository> _mockTrackingSessionRepository;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<ILogger<LunchReminderWorker>> _mockLogger;
    private readonly IOptions<WorkerOptions> _options;

    public LunchReminderWorkerTests()
    {
        _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
        _mockServiceScope = new Mock<IServiceScope>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockTrackingSessionRepository = new Mock<ITrackingSessionRepository>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockLogger = new Mock<ILogger<LunchReminderWorker>>();

        // Setup service scope factory to return mocked dependencies
        _mockServiceScope.Setup(s => s.ServiceProvider).Returns(_mockServiceProvider.Object);
        _mockServiceScopeFactory.Setup(f => f.CreateScope()).Returns(_mockServiceScope.Object);

        _mockServiceProvider
            .Setup(p => p.GetService(typeof(IUserRepository)))
            .Returns(_mockUserRepository.Object);
        _mockServiceProvider
            .Setup(p => p.GetService(typeof(ITrackingSessionRepository)))
            .Returns(_mockTrackingSessionRepository.Object);
        _mockServiceProvider
            .Setup(p => p.GetService(typeof(INotificationService)))
            .Returns(_mockNotificationService.Object);

        // Configure worker options with a short check interval for testing
        _options = Options.Create(new WorkerOptions
        {
            LunchReminderCheckInterval = TimeSpan.FromSeconds(1)
        });
    }

    [Fact]
    public async Task CheckLunchReminder_WhenUserAlreadyHadLunchToday_ShouldNotSendReminder()
    {
        // Arrange - This test reproduces the bug scenario:
        // User took lunch from 10:30-11:40, started working at 11:46,
        // and lunch reminder was incorrectly sent at 11:49

        var userId = 123456789;
        var utcOffsetMinutes = 120; // UTC+2 for example

        // Use current UTC time
        var currentUtc = DateTime.UtcNow;
        var currentLocal = currentUtc.AddMinutes(utcOffsetMinutes);

        // Calculate user's day boundaries in UTC
        var userLocalDate = DateOnly.FromDateTime(currentLocal);
        var userDayStartUtc = userLocalDate.ToDateTime(TimeOnly.MinValue).AddMinutes(-utcOffsetMinutes);
        var userDayEndUtc = userDayStartUtc.AddDays(1);

        // User configuration: lunch reminder at a time that has already passed
        var user = new User(userId, "testuser", isAdmin: true, utcOffsetMinutes: utcOffsetMinutes);
        var reminderHour = (currentLocal.Hour > 0) ? currentLocal.Hour - 1 : 23; // 1 hour ago
        user.UpdateLunchReminderTime(reminderHour, 0); // Reminder has passed

        // User's current state: Working
        var activeWorkSession = new TrackingSession(
            userId,
            TrackingState.Working,
            currentUtc.AddMinutes(-30)); // Started 30 minutes ago

        // User's completed lunch session earlier today
        var lunchStartUtc = userDayStartUtc.AddHours(10); // 10 hours into the day
        var lunchEndUtc = lunchStartUtc.AddMinutes(70); // 70 minute lunch
        var completedLunchSession = new TrackingSession(
            Guid.NewGuid(),
            userId,
            TrackingState.Lunch,
            lunchStartUtc,
            lunchEndUtc);

        // Mock repository responses
        _mockUserRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([user]);

        _mockTrackingSessionRepository
            .Setup(r => r.GetActiveSessionAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeWorkSession);

        // This is the key part - the completed lunch session should be in the range
        _mockTrackingSessionRepository
            .Setup(r => r.GetSessionsInRangeAsync(
                userId,
                userDayStartUtc,
                userDayEndUtc,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([completedLunchSession, activeWorkSession]);

        // Create worker (we can't easily test the background service execution,
        // so we'll need to use reflection to call the private method)
        var worker = new LunchReminderWorker(
            _mockServiceScopeFactory.Object,
            _mockLogger.Object,
            _options);

        // Use reflection to invoke the private CheckAndSendLunchRemindersAsync method
        var method = typeof(LunchReminderWorker).GetMethod(
            "CheckAndSendLunchRemindersAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act - Simulate the time when the bug occurred
        using var cts = new CancellationTokenSource();
        await (Task)method!.Invoke(worker, [cts.Token])!;

        // Assert - The notification should NOT have been sent because lunch was already taken
        _mockNotificationService.Verify(
            n => n.SendLunchReminderAsync(userId, It.IsAny<CancellationToken>()),
            Times.Never,
            "Lunch reminder should not be sent when user already had lunch today");
    }

    [Fact]
    public async Task CheckLunchReminder_WhenUserHasNotHadLunchAndIsWorking_ShouldSendReminder()
    {
        // Skip test on weekends since worker doesn't run on weekends
        if (DateTime.UtcNow.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return; // Skip test
        }

        // Arrange
        var userId = 123456789;
        var utcOffsetMinutes = 120; // UTC+2

        // Use current UTC time
        var currentUtc = DateTime.UtcNow;
        var currentLocal = currentUtc.AddMinutes(utcOffsetMinutes);

        // User configuration: lunch reminder at a time that has passed
        var user = new User(userId, "testuser", isAdmin: true, utcOffsetMinutes: utcOffsetMinutes);
        var reminderHour = (currentLocal.Hour > 0) ? currentLocal.Hour - 1 : 23; // 1 hour ago
        user.UpdateLunchReminderTime(reminderHour, 0);

        // User's current state: Working
        var activeWorkSession = new TrackingSession(
            userId,
            TrackingState.Working,
            currentUtc.AddHours(-2)); // Started 2 hours ago

        // No lunch session today
        var userLocalDate = DateOnly.FromDateTime(currentUtc.AddMinutes(utcOffsetMinutes));
        var userDayStartUtc = userLocalDate.ToDateTime(TimeOnly.MinValue).AddMinutes(-utcOffsetMinutes);
        var userDayEndUtc = userDayStartUtc.AddDays(1);

        _mockUserRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([user]);

        _mockTrackingSessionRepository
            .Setup(r => r.GetActiveSessionAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeWorkSession);

        // Only work session, no lunch session
        _mockTrackingSessionRepository
            .Setup(r => r.GetSessionsInRangeAsync(
                userId,
                userDayStartUtc,
                userDayEndUtc,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([activeWorkSession]);

        var worker = new LunchReminderWorker(
            _mockServiceScopeFactory.Object,
            _mockLogger.Object,
            _options);

        var method = typeof(LunchReminderWorker).GetMethod(
            "CheckAndSendLunchRemindersAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        using var cts = new CancellationTokenSource();
        await (Task)method!.Invoke(worker, [cts.Token])!;

        // Assert
        _mockNotificationService.Verify(
            n => n.SendLunchReminderAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once,
            "Lunch reminder should be sent when user hasn't had lunch yet");
    }

    [Fact]
    public async Task CheckLunchReminder_WhenUserIsNotWorking_ShouldNotSendReminder()
    {
        // Arrange
        var userId = 123456789;
        var utcOffsetMinutes = 0;

        var user = new User(userId, "testuser", isAdmin: true, utcOffsetMinutes: utcOffsetMinutes);
        user.UpdateLunchReminderTime(12, 0);

        // User is not in working state (maybe commuting or idle)
        var activeCommuteSession = new TrackingSession(
            userId,
            TrackingState.Commuting,
            DateTime.UtcNow.AddMinutes(-30),
            CommuteDirection.ToWork);

        _mockUserRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([user]);

        _mockTrackingSessionRepository
            .Setup(r => r.GetActiveSessionAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeCommuteSession);

        var worker = new LunchReminderWorker(
            _mockServiceScopeFactory.Object,
            _mockLogger.Object,
            _options);

        var method = typeof(LunchReminderWorker).GetMethod(
            "CheckAndSendLunchRemindersAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        using var cts = new CancellationTokenSource();
        await (Task)method!.Invoke(worker, [cts.Token])!;

        // Assert
        _mockNotificationService.Verify(
            n => n.SendLunchReminderAsync(userId, It.IsAny<CancellationToken>()),
            Times.Never,
            "Lunch reminder should not be sent when user is not working");
    }

    [Fact]
    public async Task CheckLunchReminder_WhenBeforeReminderTime_ShouldNotSendReminder()
    {
        // Arrange
        var userId = 123456789;
        var utcOffsetMinutes = 0;

        // Use current UTC time
        var currentUtc = DateTime.UtcNow;
        var currentLocal = currentUtc.AddMinutes(utcOffsetMinutes);

        // Lunch reminder set for 1 hour in the future (not yet time)
        var user = new User(userId, "testuser", isAdmin: true, utcOffsetMinutes: utcOffsetMinutes);
        var futureHour = (currentLocal.Hour + 1) % 24;
        user.UpdateLunchReminderTime(futureHour, 0);

        var activeWorkSession = new TrackingSession(
            userId,
            TrackingState.Working,
            currentUtc.AddHours(-2));

        _mockUserRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([user]);

        _mockTrackingSessionRepository
            .Setup(r => r.GetActiveSessionAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeWorkSession);

        var worker = new LunchReminderWorker(
            _mockServiceScopeFactory.Object,
            _mockLogger.Object,
            _options);

        var method = typeof(LunchReminderWorker).GetMethod(
            "CheckAndSendLunchRemindersAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        using var cts = new CancellationTokenSource();
        await (Task)method!.Invoke(worker, [cts.Token])!;

        // Assert
        _mockNotificationService.Verify(
            n => n.SendLunchReminderAsync(userId, It.IsAny<CancellationToken>()),
            Times.Never,
            "Lunch reminder should not be sent before the configured time");
    }

    [Fact]
    public async Task CheckLunchReminder_OnWeekend_ShouldNotSendReminder()
    {
        // Arrange
        var userId = 123456789;
        var utcOffsetMinutes = 0;

        // Find next Saturday to test weekend logic
        var currentUtc = DateTime.UtcNow;
        var daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)currentUtc.DayOfWeek + 7) % 7;
        if (daysUntilSaturday == 0 && currentUtc.DayOfWeek != DayOfWeek.Saturday)
            daysUntilSaturday = 7;
        var saturdayUtc = currentUtc.Date.AddDays(daysUntilSaturday).AddHours(12);

        var user = new User(userId, "testuser", isAdmin: true, utcOffsetMinutes: utcOffsetMinutes);
        user.UpdateLunchReminderTime(11, 0);

        var activeWorkSession = new TrackingSession(
            userId,
            TrackingState.Working,
            saturdayUtc.AddHours(-2));

        _mockUserRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([user]);

        _mockTrackingSessionRepository
            .Setup(r => r.GetActiveSessionAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeWorkSession);

        var worker = new LunchReminderWorker(
            _mockServiceScopeFactory.Object,
            _mockLogger.Object,
            _options);

        var method = typeof(LunchReminderWorker).GetMethod(
            "CheckAndSendLunchRemindersAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        using var cts = new CancellationTokenSource();
        await (Task)method!.Invoke(worker, [cts.Token])!;

        // Assert
        _mockNotificationService.Verify(
            n => n.SendLunchReminderAsync(userId, It.IsAny<CancellationToken>()),
            Times.Never,
            "Lunch reminder should not be sent on weekends");
    }
}
