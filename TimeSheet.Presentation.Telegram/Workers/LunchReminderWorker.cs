using System.Collections.Concurrent;
using TimeSheet.Core.Application.Interfaces;

namespace TimeSheet.Presentation.Telegram.Workers;

/// <summary>
/// Background worker that periodically checks if users need to be reminded to take lunch.
/// Checks every 15 minutes and sends reminders to users who:
/// - Have configured a lunch reminder hour
/// - Are currently in "working" state
/// - Haven't taken lunch yet today
/// - Haven't been reminded yet today
/// - Current user time is past the configured reminder hour
/// </summary>
public sealed class LunchReminderWorker(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<LunchReminderWorker> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(15);
    private readonly ConcurrentDictionary<long, DateOnly> _remindersSentToday = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Lunch reminder worker started. Check interval: {Interval} minutes", CheckInterval.TotalMinutes);

        // Wait a bit before starting the first check to allow the bot to fully initialize
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndSendLunchRemindersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error checking for lunch reminders");
            }

            // Wait for the next check interval
            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    /// <summary>
    /// Checks all users and sends lunch reminders to those who need them.
    /// </summary>
    private async Task CheckAndSendLunchRemindersAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var trackingSessionRepository = scope.ServiceProvider.GetRequiredService<ITrackingSessionRepository>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        // Get all users with lunch reminder configured
        var allUsers = await userRepository.GetAllAsync(cancellationToken);
        var usersWithReminder = allUsers.Where(u => u.LunchReminderHour.HasValue).ToList();

        if (usersWithReminder.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var remindersCount = 0;

        // Clean up old reminder records (from previous days)
        CleanupOldReminderRecords(today);

        foreach (var user in usersWithReminder)
        {
            try
            {
                // Check if we've already reminded this user today
                if (_remindersSentToday.TryGetValue(user.TelegramUserId, out var lastReminderDate) &&
                    lastReminderDate == today)
                {
                    continue; // Already reminded today
                }

                // Calculate user's current local time
                var userLocalTime = now.AddMinutes(user.UtcOffsetMinutes);
                var userLocalHour = userLocalTime.Hour;
                var userLocalMinute = userLocalTime.Minute;
                var userLocalDate = DateOnly.FromDateTime(userLocalTime);

                // Check if it's past the reminder time (hour and minute)
                var reminderHour = user.LunchReminderHour!.Value;
                var reminderMinute = user.LunchReminderMinute;

                // Convert both to total minutes for easier comparison
                var currentMinutes = userLocalHour * 60 + userLocalMinute;
                var reminderMinutes = reminderHour * 60 + reminderMinute;

                if (currentMinutes < reminderMinutes)
                {
                    continue; // Not yet time to remind
                }

                // Check if user is currently working
                var activeSession = await trackingSessionRepository.GetActiveSessionAsync(
                    user.TelegramUserId,
                    cancellationToken);

                if (activeSession == null || activeSession.State != Core.Domain.Enums.TrackingState.Working)
                {
                    continue; // Not currently working
                }

                // Check if user has already taken lunch today (in their local timezone)
                var userDayStartUtc = userLocalDate.ToDateTime(TimeOnly.MinValue).AddMinutes(-user.UtcOffsetMinutes);
                var userDayEndUtc = userDayStartUtc.AddDays(1);

                var todaySessions = await trackingSessionRepository.GetSessionsByDateAsync(
                    user.TelegramUserId,
                    userDayStartUtc,
                    cancellationToken);

                var hasLunchToday = todaySessions.Any(s =>
                    s.State == Core.Domain.Enums.TrackingState.Lunch &&
                    s.StartedAt >= userDayStartUtc &&
                    s.StartedAt < userDayEndUtc);

                if (hasLunchToday)
                {
                    continue; // User already took lunch today
                }

                // Send the reminder
                await notificationService.SendLunchReminderAsync(user.TelegramUserId, cancellationToken);

                // Mark as reminded today
                _remindersSentToday[user.TelegramUserId] = today;
                remindersCount++;

                logger.LogInformation(
                    "Sent lunch reminder to user {UserId} (local time: {LocalTime:HH:mm})",
                    user.TelegramUserId,
                    userLocalTime);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing lunch reminder for user {UserId}", user.TelegramUserId);
            }
        }

        if (remindersCount > 0)
        {
            logger.LogInformation("Sent {Count} lunch reminder(s)", remindersCount);
        }
    }

    /// <summary>
    /// Removes reminder records from previous days to prevent memory growth.
    /// </summary>
    private void CleanupOldReminderRecords(DateOnly today)
    {
        var usersToRemove = _remindersSentToday
            .Where(kvp => kvp.Value < today)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var userId in usersToRemove)
        {
            _remindersSentToday.TryRemove(userId, out _);
        }

        if (usersToRemove.Count > 0)
        {
            logger.LogDebug("Cleaned up {Count} old reminder records", usersToRemove.Count);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Lunch reminder worker is stopping...");
        return base.StopAsync(cancellationToken);
    }
}
