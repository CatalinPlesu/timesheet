using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TimeSheet.Core.Application.Interfaces;

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
}
