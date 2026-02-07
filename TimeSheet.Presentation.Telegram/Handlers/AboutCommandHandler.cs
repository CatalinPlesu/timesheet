using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TimeSheet.Presentation.Telegram.Handlers;

/// <summary>
/// Handles the /about command, which shows bot information.
/// This is the only command available to non-registered users.
/// </summary>
public class AboutCommandHandler(ILogger<AboutCommandHandler> logger)
{
    private const string AboutMessage = """
        📊 *TimeSheet Bot*

        A private time-tracking bot to help you manage work hours, commute time, and breaks.

        This bot is *not* for employer surveillance — it's a personal tool to ensure work doesn't take more time than it should.

        *Registration Required*
        This is a private bot. You need a registration code from the administrator to use it.

        *Self-Hosting*
        This bot is open-source. Deploy your own instance:
        🔗 [github.com/CatalinPlesu/timesheet](https://github.com/CatalinPlesu/timesheet)
        """;

    /// <summary>
    /// Handles the /about command.
    /// </summary>
    public async Task HandleAboutAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "About command from user {UserId} ({Username})",
            message.From?.Id ?? 0,
            message.From?.Username ?? "Unknown");

        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: AboutMessage,
            parseMode: ParseMode.Markdown,
            cancellationToken: cancellationToken);
    }
}
