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
        *TimeSheet*

        A personal work-life balance tool. Track your own hours so work doesn't quietly take more of your day than it should.

        Log commutes, work sessions, and breaks with a simple command. Check weekly and monthly summaries to stay aware of patterns — and step back when needed.

        Your data stays on your own server. No telemetry, no third parties.

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
        Open-source — deploy your own instance:
        [github.com/CatalinPlesu/timesheet](https://github.com/CatalinPlesu/timesheet)
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
