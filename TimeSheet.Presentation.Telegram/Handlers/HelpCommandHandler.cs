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
        📊 *TimeSheet Bot - Help*

        *Core Commands:*
        `/commute` (or `/c`) — Start/stop commute tracking
        `/work` (or `/w`) — Start/stop work session
        `/lunch` (or `/l`) — Start/stop lunch break

        *Optional Time Parameters:*
        All commands support optional time parameters:
        • `-m` — started m minutes ago (e.g., `/work -15`)
        • `+m` — will start m minutes from now (e.g., `/lunch +5`)
        • `[HH:MM]` — exact time in 24h format (e.g., `/commute 08:30`, `/work [09:00]`)

        *Examples:*
        • `/work` — start working now
        • `/work -10` — started working 10 minutes ago
        • `/lunch 12:30` — lunch started at 12:30
        • `/commute` — stop commuting (toggle behavior)

        *How It Works:*
        • Commands are *exclusive* — starting a new state stops the previous one
        • Repeating the same command *stops* it (toggle behavior)
        • Valid sequences: `/commute` → `/work` → `/lunch` → `/work` → `/commute`

        *Viewing Entries:*
        `/status` (or `/s`) — Show current tracking status
        `/list` — Show today's time entries

        *Editing & Corrections:*
        `/edit` — Edit most recent entry (or `/edit N` for N entries back)
        `/delete` — Delete most recent entry (or `/delete N` for N entries back)

        *Settings:*
        `/settings` — View and change your settings (timezone, etc.)

        *Reports & Analytics:*
        `/dailystats` — View average daily statistics (last 7, 30, 90 days)
        `/commutepatterns` — Analyze commute patterns by day of week
        `/report [period]` — View aggregated statistics
          • `/report week` — Current week totals
          • `/report month` — Current month totals
          • `/report year` — Current year totals

        *Admin Commands:*
        `/generate` — Generate a new registration mnemonic (admin-only)

        *Other Commands:*
        `/about` — Bot information
        `/help` — This help message
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
