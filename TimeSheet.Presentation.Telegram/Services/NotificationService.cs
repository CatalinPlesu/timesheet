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
}
