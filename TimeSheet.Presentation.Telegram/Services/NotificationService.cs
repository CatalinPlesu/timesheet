using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TimeSheet.Core.Application.Common;
using TimeSheet.Core.Application.Interfaces.Services;
using TimeSheet.Core.Application.Models;
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

                *Target:* {TimeFormatter.FormatDuration(targetHours)}
                *Worked:* {TimeFormatter.FormatDuration(actualHours)}

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
    public async Task SendAutoShutdownNotificationAsync(
        long telegramUserId,
        TrackingState state,
        TimeSpan duration,
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

            var durationText = TimeFormatter.FormatDuration(duration);

            var message = $"""
                ⚠️ *Auto-Shutdown*

                Your {stateName} was automatically ended after {durationText}.

                Use /status to see your current state.
                """;

            await botClient.SendMessage(
                chatId: telegramUserId,
                text: message,
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "Sent auto-shutdown notification to user {UserId} (state: {State}, duration: {Duration})",
                telegramUserId,
                state,
                duration);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to send auto-shutdown notification to user {UserId}",
                telegramUserId);
        }
    }

    /// <inheritdoc/>
    public async Task SendEndOfDaySummaryAsync(
        long telegramUserId,
        PeriodAggregate daySummary,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var message = FormatEndOfDaySummary(daySummary);

            await botClient.SendMessage(
                chatId: telegramUserId,
                text: message,
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken);

            logger.LogInformation("Sent end-of-day summary to user {UserId}", telegramUserId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send end-of-day summary to user {UserId}", telegramUserId);
        }
    }

    /// <summary>
    /// Formats the end-of-day summary message.
    /// </summary>
    private static string FormatEndOfDaySummary(PeriodAggregate summary)
    {
        var builder = new StringBuilder();
        builder.AppendLine("📋 *End of Day Summary*");
        builder.AppendLine();

        if (summary.WorkDaysCount == 0 || summary.TotalWorkHours == 0)
        {
            builder.AppendLine("No work sessions recorded today.");
            return builder.ToString();
        }

        builder.AppendLine($"*Work:* {TimeFormatter.FormatDuration(summary.TotalWorkHours)}");

        if (summary.TotalCommuteHours > 0)
        {
            builder.AppendLine($"*Commute:* {TimeFormatter.FormatDuration(summary.TotalCommuteHours)}");
        }

        if (summary.TotalLunchHours > 0)
        {
            builder.AppendLine($"*Lunch:* {TimeFormatter.FormatDuration(summary.TotalLunchHours)}");
        }

        if (summary.TotalDurationHours.HasValue && summary.TotalDurationHours.Value > 0)
        {
            builder.AppendLine($"*Total span:* {TimeFormatter.FormatDuration(summary.TotalDurationHours.Value)}");
        }

        return builder.ToString().TrimEnd();
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

                Your {stateName} has been running for {TimeFormatter.FormatDuration(currentDuration)}.
                Your average {stateName} duration is {TimeFormatter.FormatDuration(averageDuration)}.

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
