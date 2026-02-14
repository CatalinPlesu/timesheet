using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using TimeSheet.Core.Application.Interfaces;
using TimeSheet.Presentation.Telegram.Options;

namespace TimeSheet.Presentation.Telegram.Workers;

/// <summary>
/// Background worker that periodically checks if users have reached their target work hours.
/// Checks periodically and sends notifications to users who:
/// - Have configured a target work hours setting
/// - Have reached or exceeded their target hours for today
/// - Haven't been notified yet today
/// </summary>
public sealed class WorkHoursAlertWorker(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<WorkHoursAlertWorker> logger,
    IOptions<WorkerOptions> options) : BackgroundService
{
    private TimeSpan CheckInterval => options.Value.WorkHoursAlertCheckInterval;
    private readonly ConcurrentDictionary<long, DateOnly> _alertsSentToday = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Work hours alert worker started. Check interval: {Interval} minutes", CheckInterval.TotalMinutes);

        // Wait a bit before starting the first check to allow the bot to fully initialize
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndSendWorkHoursAlertsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error checking for work hours alerts");
            }

            // Wait for the next check interval
            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    /// <summary>
    /// Checks all users and sends work hours complete alerts to those who have reached their target.
    /// </summary>
    private async Task CheckAndSendWorkHoursAlertsAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var trackingSessionRepository = scope.ServiceProvider.GetRequiredService<ITrackingSessionRepository>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        // Get all users with target work hours configured
        var allUsers = await userRepository.GetAllAsync(cancellationToken);
        var usersWithTarget = allUsers.Where(u => u.TargetWorkHours.HasValue).ToList();

        if (usersWithTarget.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var alertsCount = 0;

        // Clean up old alert records (from previous days)
        // Note: This cleanup is conservative - it only removes entries that are definitely old
        // (more than 2 days old) to avoid timezone-related issues
        CleanupOldAlertRecords(now);

        foreach (var user in usersWithTarget)
        {
            try
            {
                // Calculate user's local date
                var userLocalTime = now.AddMinutes(user.UtcOffsetMinutes);
                var userLocalDate = DateOnly.FromDateTime(userLocalTime);

                // Skip weekend notifications
                if (userLocalTime.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                    continue;

                // Check if we've already alerted this user today (using user's local date)
                if (_alertsSentToday.TryGetValue(user.TelegramUserId, out var lastAlertDate) &&
                    lastAlertDate == userLocalDate)
                {
                    continue; // Already alerted today
                }

                // Calculate the start of the user's day in UTC
                var userDayStartUtc = userLocalDate.ToDateTime(TimeOnly.MinValue).AddMinutes(-user.UtcOffsetMinutes);

                // Get total work hours for the user's current day
                var totalWorkHours = await trackingSessionRepository.GetTotalWorkHoursForDayAsync(
                    user.TelegramUserId,
                    userDayStartUtc,
                    cancellationToken);

                // Check if user has reached their target
                if (totalWorkHours >= user.TargetWorkHours!.Value)
                {
                    // Send the alert
                    await notificationService.SendWorkHoursCompleteAsync(
                        user.TelegramUserId,
                        user.TargetWorkHours.Value,
                        totalWorkHours,
                        cancellationToken);

                    // Mark as alerted today (using user's local date)
                    _alertsSentToday[user.TelegramUserId] = userLocalDate;
                    alertsCount++;

                    logger.LogInformation(
                        "Sent work hours complete alert to user {UserId} (target: {Target}h, actual: {Actual}h)",
                        user.TelegramUserId,
                        user.TargetWorkHours.Value,
                        totalWorkHours);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing work hours alert for user {UserId}", user.TelegramUserId);
            }
        }

        if (alertsCount > 0)
        {
            logger.LogInformation("Sent {Count} work hours complete alert(s)", alertsCount);
        }
    }

    /// <summary>
    /// Removes alert records from previous days to prevent memory growth.
    /// Uses a conservative approach: removes entries older than 2 days to avoid
    /// timezone-related issues (since users can be in different timezones).
    /// </summary>
    private void CleanupOldAlertRecords(DateTime nowUtc)
    {
        // Calculate a conservative cutoff: 2 days ago
        // This ensures we don't accidentally remove today's alerts for users
        // in very different timezones (e.g., UTC-12 to UTC+14 is 26 hours difference)
        var cutoffDate = DateOnly.FromDateTime(nowUtc).AddDays(-2);

        var usersToRemove = _alertsSentToday
            .Where(kvp => kvp.Value < cutoffDate)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var userId in usersToRemove)
        {
            _alertsSentToday.TryRemove(userId, out _);
        }

        if (usersToRemove.Count > 0)
        {
            logger.LogDebug("Cleaned up {Count} old alert records", usersToRemove.Count);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Work hours alert worker is stopping...");
        return base.StopAsync(cancellationToken);
    }
}
