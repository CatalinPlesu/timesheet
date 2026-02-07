using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TimeSheet.Core.Application.Interfaces;
using TimeSheet.Core.Domain.Enums;

namespace TimeSheet.Presentation.Telegram.Services;

/// <summary>
/// Service for sending notifications to users via Telegram.
/// </summary>
public sealed class NotificationService(
    ITelegramBotClient botClient,
    ILogger<NotificationService> logger) : INotificationService
{
    /// <inheritdoc/>
    public async Task SendLunchReminderAsync(long telegramUserId, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = """
                🍽️ *Lunch Reminder*

                Don't forget to take your lunch break!

                Use `/lunch` or `/l` to start tracking your lunch time.
                """;

            await botClient.SendMessage(
                chatId: telegramUserId,
                text: message,
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken);

            logger.LogInformation("Sent lunch reminder to user {UserId}", telegramUserId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send lunch reminder to user {UserId}", telegramUserId);
        }
    }

    /// <inheritdoc/>
    public async Task SendWorkHoursCompleteAsync(
        long telegramUserId,
        decimal targetHours,
        decimal actualHours,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var message = $"""
                ✅ *Work Hours Complete*

                You've reached your target work hours for today!

                *Target:* {targetHours:F1} hours
                *Worked:* {actualHours:F1} hours

                Great job! 🎉
                """;

            await botClient.SendMessage(
                chatId: telegramUserId,
                text: message,
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "Sent work hours complete notification to user {UserId} (target: {Target}h, actual: {Actual}h)",
                telegramUserId,
                targetHours,
                actualHours);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to send work hours complete notification to user {UserId}",
                telegramUserId);
        }
    }

    /// <inheritdoc/>
    public async Task SendForgotShutdownReminderAsync(
        long telegramUserId,
        TrackingState state,
        decimal currentDuration,
        decimal averageDuration,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stateName = state switch
            {
                TrackingState.Working => "work session",
                TrackingState.Commuting => "commute",
                TrackingState.Lunch => "lunch break",
                _ => "session"
            };

            var stateEmoji = state switch
            {
                TrackingState.Working => "💼",
                TrackingState.Commuting => "🚗",
                TrackingState.Lunch => "🍽️",
                _ => "⏱️"
            };

            var message = $"""
                {stateEmoji} *Forgot to Stop Tracking?*

                Your {stateName} has been running for {currentDuration:F1} hours.
                Your average {stateName} duration is {averageDuration:F1} hours.

                This is unusually long. Did you forget to stop tracking?

                Use the appropriate command to end this session:
                • `/work` to toggle work
                • `/commute` or `/c` to toggle commute
                • `/lunch` or `/l` to toggle lunch
                """;

            await botClient.SendMessage(
                chatId: telegramUserId,
                text: message,
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "Sent forgot-shutdown reminder to user {UserId} (state: {State}, current: {Current}h, average: {Average}h)",
                telegramUserId,
                state,
                currentDuration,
                averageDuration);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to send forgot-shutdown reminder to user {UserId}",
                telegramUserId);
        }
    }
}
