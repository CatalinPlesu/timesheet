using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TimeSheet.Presentation.Telegram.Handlers;

/// <summary>
/// Handles the /help command, which explains how to use the bot.
/// </summary>
public class HelpCommandHandler(ILogger<HelpCommandHandler> logger)
{
    private const string HelpMessage = """
        📊 *TimeSheet Bot - Quick Reference*

        ━━━━━━━━━━━━━━━━━━━━━━━━━━
        *⏱️  TRACKING*
        ━━━━━━━━━━━━━━━━━━━━━━━━━━
        `/commute` or `/c` — Track commute
        `/work` or `/w` — Track work time
        `/lunch` or `/l` — Track lunch break
        `/status` or `/s` — Show current status

        💡 *Tip:* Commands toggle on/off. Starting a new state stops the previous one.

        ━━━━━━━━━━━━━━━━━━━━━━━━━━
        *🕒  TIME OPTIONS*
        ━━━━━━━━━━━━━━━━━━━━━━━━━━
        `/work -15` — Started 15 min ago
        `/lunch +5` — Starts in 5 min
        `/commute 08:30` — Started at 8:30

        ━━━━━━━━━━━━━━━━━━━━━━━━━━
        *📝  VIEW & EDIT*
        ━━━━━━━━━━━━━━━━━━━━━━━━━━
        `/list` — Today's entries
        `/edit` — Adjust recent entry
        `/delete` — Remove entry

        ━━━━━━━━━━━━━━━━━━━━━━━━━━
        *📊  REPORTS*
        ━━━━━━━━━━━━━━━━━━━━━━━━━━
        `/report day` — Today's summary
        `/report week` — This week
        `/report month` — This month
        `/report year` — This year
        `/report commute` — Commute patterns

        ━━━━━━━━━━━━━━━━━━━━━━━━━━
        *⚙️  SETTINGS*
        ━━━━━━━━━━━━━━━━━━━━━━━━━━
        `/settings` — Configure timezone, reminders, etc.
        `/about` — Bot information
        `/help` — This message
        """;

    /// <summary>
    /// Handles the /help command.
    /// </summary>
    public async Task HandleHelpAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Help command from user {UserId} ({Username})",
            message.From?.Id ?? 0,
            message.From?.Username ?? "Unknown");

        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: HelpMessage,
            parseMode: ParseMode.Markdown,
            cancellationToken: cancellationToken);
    }
}
