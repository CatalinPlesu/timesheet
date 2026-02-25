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

        A personal time-awareness tool to help you understand your daily work and commute patterns, stay in control of your time, and maintain a healthy work-life balance.

        Track your work sessions, commutes, and breaks — then use the reports to spot trends, see where your time goes, and make informed decisions about your day.

        *Command Aliases*
        `/c` — /commute    `/w` — /work      `/l` — /lunch
        `/s` — /status     `/e` — /edit      `/d` — /delete
        `/n` — /note       `/r` — /report    `/se` — /settings
        `/li` — /list      `/i` — /import    `/lo` — /login
        `/g` — /generate   `/re` — /register
        `/h` — /help       `/a` — /about

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
