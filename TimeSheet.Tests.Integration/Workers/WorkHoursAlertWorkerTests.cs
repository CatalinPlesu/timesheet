namespace TimeSheet.Tests.Integration.Workers;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TimeSheet.Core.Application.Interfaces;
using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.Enums;
using TimeSheet.Presentation.Telegram.Options;
using TimeSheet.Presentation.Telegram.Workers;

public class WorkHoursAlertWorkerTests
{
    private readonly Mock<IServiceScopeFactory> _mockServiceScopeFactory;
    private readonly Mock<IServiceScope> _mockServiceScope;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ITrackingSessionRepository> _mockTrackingSessionRepository;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<ILogger<WorkHoursAlertWorker>> _mockLogger;
    private readonly IOptions<WorkerOptions> _options;

    public WorkHoursAlertWorkerTests()
    {
        _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
        _mockServiceScope = new Mock<IServiceScope>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockTrackingSessionRepository = new Mock<ITrackingSessionRepository>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockLogger = new Mock<ILogger<WorkHoursAlertWorker>>();

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
            WorkHoursAlertCheckInterval = TimeSpan.FromSeconds(1)
        });
    }

    [Fact]
    public async Task CheckWorkHoursAlert_WhenUserReachesTargetDuringActiveSession_ShouldSendNotification()
    {
        // Arrange - This test verifies the bug fix: user should be notified when total working time
        // (recorded + still tracking) exceeds daily limit, even during an active session

        var userId = 123456789;
        var utcOffsetMinutes = 120; // UTC+2

        // User configuration: target work hours = 8.0
        var user = new User(userId, "testuser", isAdmin: true, utcOffsetMinutes: utcOffsetMinutes);
        user.UpdateTargetWorkHours(8.0m);

        // Current time: 17:00 local (15:00 UTC with +2 offset)
        var currentUtc = new DateTime(2026, 2, 10, 15, 0, 0, DateTimeKind.Utc);
        var currentLocal = currentUtc.AddMinutes(utcOffsetMinutes); // 17:00 local

        // Calculate user's day boundaries in UTC
        var userLocalDate = DateOnly.FromDateTime(currentLocal);
        var userDayStartUtc = userLocalDate.ToDateTime(TimeOnly.MinValue).AddMinutes(-utcOffsetMinutes);

        // User started working at 09:00 local (07:00 UTC) and has been working continuously for 8 hours
        // The session is still active (EndedAt = null)
        var workStartUtc = new DateTime(2026, 2, 10, 7, 0, 0, DateTimeKind.Utc); // 09:00 local

        // Mock repository responses
        _mockUserRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([user]);

        // GetTotalWorkHoursForDayAsync should return 8.0 hours (including the active session)
        _mockTrackingSessionRepository
            .Setup(r => r.GetTotalWorkHoursForDayAsync(
                userId,
                userDayStartUtc,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(8.0m); // 8 hours of work

        // Create worker
        var worker = new WorkHoursAlertWorker(
            _mockServiceScopeFactory.Object,
            _mockLogger.Object,
            _options);

        // Use reflection to invoke the private CheckAndSendWorkHoursAlertsAsync method
        var method = typeof(WorkHoursAlertWorker).GetMethod(
            "CheckAndSendWorkHoursAlertsAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        using var cts = new CancellationTokenSource();
        await (Task)method!.Invoke(worker, [cts.Token])!;

        // Assert - The notification SHOULD be sent because the user reached their target
        _mockNotificationService.Verify(
            n => n.SendWorkHoursCompleteAsync(
                userId,
                8.0m,
                8.0m,
                It.IsAny<CancellationToken>()),
            Times.Once,
            "User should be notified when reaching target work hours during an active session");
    }

    [Fact]
    public async Task CheckWorkHoursAlert_WhenUserHasNotReachedTarget_ShouldNotSendNotification()
    {
        // Arrange
        var userId = 123456789;
        var utcOffsetMinutes = 120; // UTC+2

        // User configuration: target work hours = 8.0
        var user = new User(userId, "testuser", isAdmin: true, utcOffsetMinutes: utcOffsetMinutes);
        user.UpdateTargetWorkHours(8.0m);

        var currentUtc = new DateTime(2026, 2, 10, 15, 0, 0, DateTimeKind.Utc);
        var currentLocal = currentUtc.AddMinutes(utcOffsetMinutes);
        var userLocalDate = DateOnly.FromDateTime(currentLocal);
        var userDayStartUtc = userLocalDate.ToDateTime(TimeOnly.MinValue).AddMinutes(-utcOffsetMinutes);

        _mockUserRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([user]);

        // User has only worked 6 hours so far
        _mockTrackingSessionRepository
            .Setup(r => r.GetTotalWorkHoursForDayAsync(
                userId,
                userDayStartUtc,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(6.0m);

        var worker = new WorkHoursAlertWorker(
            _mockServiceScopeFactory.Object,
            _mockLogger.Object,
            _options);

        var method = typeof(WorkHoursAlertWorker).GetMethod(
            "CheckAndSendWorkHoursAlertsAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        using var cts = new CancellationTokenSource();
        await (Task)method!.Invoke(worker, [cts.Token])!;

        // Assert - No notification should be sent
        _mockNotificationService.Verify(
            n => n.SendWorkHoursCompleteAsync(
                It.IsAny<long>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<CancellationToken>()),
            Times.Never,
            "User should not be notified when target not yet reached");
    }

    [Fact]
    public async Task CheckWorkHoursAlert_WhenUserHasNoTargetConfigured_ShouldNotSendNotification()
    {
        // Arrange
        var userId = 123456789;
        var utcOffsetMinutes = 120;

        // User has no target work hours configured
        var user = new User(userId, "testuser", isAdmin: true, utcOffsetMinutes: utcOffsetMinutes);
        // No UpdateTargetWorkHours call - TargetWorkHours is null

        _mockUserRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([user]);

        var worker = new WorkHoursAlertWorker(
            _mockServiceScopeFactory.Object,
            _mockLogger.Object,
            _options);

        var method = typeof(WorkHoursAlertWorker).GetMethod(
            "CheckAndSendWorkHoursAlertsAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        using var cts = new CancellationTokenSource();
        await (Task)method!.Invoke(worker, [cts.Token])!;

        // Assert - No notification should be sent
        _mockNotificationService.Verify(
            n => n.SendWorkHoursCompleteAsync(
                It.IsAny<long>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<CancellationToken>()),
            Times.Never,
            "User should not be notified when no target is configured");
    }

    [Fact]
    public async Task CheckWorkHoursAlert_OnWeekend_ShouldNotSendNotification()
    {
        // Arrange
        var userId = 123456789;
        var utcOffsetMinutes = 0;

        // Saturday, 15:00 UTC
        var saturdayUtc = new DateTime(2026, 2, 14, 15, 0, 0, DateTimeKind.Utc); // Saturday

        var user = new User(userId, "testuser", isAdmin: true, utcOffsetMinutes: utcOffsetMinutes);
        user.UpdateTargetWorkHours(8.0m);

        var userLocalDate = DateOnly.FromDateTime(saturdayUtc);
        var userDayStartUtc = userLocalDate.ToDateTime(TimeOnly.MinValue).AddMinutes(-utcOffsetMinutes);

        _mockUserRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([user]);

        // User has worked 8 hours on Saturday
        _mockTrackingSessionRepository
            .Setup(r => r.GetTotalWorkHoursForDayAsync(
                userId,
                userDayStartUtc,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(8.0m);

        var worker = new WorkHoursAlertWorker(
            _mockServiceScopeFactory.Object,
            _mockLogger.Object,
            _options);

        var method = typeof(WorkHoursAlertWorker).GetMethod(
            "CheckAndSendWorkHoursAlertsAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        using var cts = new CancellationTokenSource();
        await (Task)method!.Invoke(worker, [cts.Token])!;

        // Assert - No notification should be sent on weekends
        _mockNotificationService.Verify(
            n => n.SendWorkHoursCompleteAsync(
                It.IsAny<long>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<CancellationToken>()),
            Times.Never,
            "Work hours alert should not be sent on weekends");
    }

    [Fact]
    public async Task CheckWorkHoursAlert_WhenAlreadyNotifiedToday_ShouldNotSendNotificationAgain()
    {
        // Arrange - Test that we only notify once per day
        var userId = 123456789;
        var utcOffsetMinutes = 120;

        var user = new User(userId, "testuser", isAdmin: true, utcOffsetMinutes: utcOffsetMinutes);
        user.UpdateTargetWorkHours(8.0m);

        var currentUtc = new DateTime(2026, 2, 10, 15, 0, 0, DateTimeKind.Utc);
        var currentLocal = currentUtc.AddMinutes(utcOffsetMinutes);
        var userLocalDate = DateOnly.FromDateTime(currentLocal);
        var userDayStartUtc = userLocalDate.ToDateTime(TimeOnly.MinValue).AddMinutes(-utcOffsetMinutes);

        _mockUserRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([user]);

        // User has worked 9 hours (exceeded target)
        _mockTrackingSessionRepository
            .Setup(r => r.GetTotalWorkHoursForDayAsync(
                userId,
                userDayStartUtc,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(9.0m);

        var worker = new WorkHoursAlertWorker(
            _mockServiceScopeFactory.Object,
            _mockLogger.Object,
            _options);

        var method = typeof(WorkHoursAlertWorker).GetMethod(
            "CheckAndSendWorkHoursAlertsAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act - Call twice in the same day
        using var cts = new CancellationTokenSource();
        await (Task)method!.Invoke(worker, [cts.Token])!;
        await (Task)method!.Invoke(worker, [cts.Token])!;

        // Assert - Notification should only be sent once
        _mockNotificationService.Verify(
            n => n.SendWorkHoursCompleteAsync(
                userId,
                8.0m,
                9.0m,
                It.IsAny<CancellationToken>()),
            Times.Once,
            "User should only be notified once per day even if target is exceeded multiple times");
    }
}
