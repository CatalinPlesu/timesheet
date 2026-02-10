using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TimeSheet.Presentation.Telegram.Handlers;

/// <summary>
/// Handles the /help command, which explains how to use the bot.
/// Supports submenus: /help tracking, /help report, /help settings.
/// </summary>
public class HelpCommandHandler(ILogger<HelpCommandHandler> logger)
{
    private const string MainHelpMessage = """
        *TimeSheet Bot*

        *Tracking:* `/c` `/w` `/l` `/s` — commute, work, lunch, status
        *View & Edit:* `/li` `/e` `/d` — list, edit, delete
        *Reports:* `/r` [day|week|month|year|commute|daily|all]
        *Settings:* `/se` — timezone, reminders, alerts
        *Other:* `/a` `/h` — about, help

        For details: `/help tracking`, `/help report`, `/help settings`
        """;

    private const string TrackingHelpMessage = """
        *Tracking Commands*

        `/commute` or `/c` — Track commute
        `/work` or `/w` — Track work time
        `/lunch` or `/l` — Track lunch break
        `/status` or `/s` — Show current status

        *Time options* (apply to any tracking command):
        `/work -15` — started 15 min ago
        `/lunch +5` — starts in 5 min
        `/commute 08:30` — started at 08:30

        *Toggle behavior:*
        Commands are exclusive — starting a new state stops the previous one.
        Repeating the same command stops it.
        Example: `/c` → `/w` → `/l` → `/w` → `/c`
        """;

    private const string ReportHelpMessage = """
        *Report Commands*

        `/report day` or `/r day` — Today's summary
        `/report week` or `/r week` — This week
        `/report month` or `/r month` — This month
        `/report year` or `/r year` — This year
        `/report commute` or `/r commute` — Commute patterns
        `/report daily` or `/r daily` — Daily averages (7/30/90 days)
        `/report table week` or `/r table week` — Daily table for week
        `/report table month` or `/r table month` — Daily table for month
        `/report all` or `/r all` — All reports as separate messages
        """;

    private const string SettingsHelpMessage = """
        *Settings Usage*

        *Timezone:*
        `/settings utc [±hours]`
        • `/settings utc +2` — Set to UTC+2
        • `/settings utc -5` — Set to UTC-5
        • `/settings utc 0` — Set to UTC+0

        *Lunch reminder:*
        `/settings lunch [hour:minute]` or `/settings lunch off`
        • `/settings lunch 12` — Remind at 12:00
        • `/settings lunch 12:30` — Remind at 12:30
        • `/settings lunch off` — Disable reminder

        *Target work hours:*
        `/settings target [hours]` or `/settings target off`
        • `/settings target 8` — Notify when 8 hours worked
        • `/settings target 7.5` — Notify when 7.5 hours worked
        • `/settings target off` — Disable notification

        *Forgot-shutdown alert:*
        `/settings forgot [percent]` or `/settings forgot off`
        • `/settings forgot 150` — Alert at 150% of average
        • `/settings forgot off` — Disable alert
        """;

    /// <summary>
    /// Handles the /help command with optional subtopic argument.
    /// </summary>
    public async Task HandleHelpAsync(
        ITelegramBotClient botClient,
        Message message,
        string[] commandParts,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Help command from user {UserId} ({Username})",
            message.From?.Id ?? 0,
            message.From?.Username ?? "Unknown");

        var subtopic = commandParts.Length >= 2 ? commandParts[1].ToLowerInvariant() : null;

        var text = subtopic switch
        {
            "tracking" => TrackingHelpMessage,
            "report" => ReportHelpMessage,
            "settings" => SettingsHelpMessage,
            _ => MainHelpMessage
        };

        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: text,
            parseMode: ParseMode.Markdown,
            cancellationToken: cancellationToken);
    }
}
