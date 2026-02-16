using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TimeSheet.Core.Application.Interfaces.Services;
using TimeSheet.Core.Application.Interfaces.Persistence;
using TimeSheet.Core.Domain.Repositories;

namespace TimeSheet.Presentation.Telegram.Handlers;

/// <summary>
/// Handles the /help command, which explains how to use the bot.
/// Supports submenus: /help tracking, /help report, /help settings, /help admin.
/// Shows context-aware help based on user role (admin vs regular user).
/// </summary>
public class HelpCommandHandler(
    ILogger<HelpCommandHandler> logger,
    IServiceScopeFactory serviceScopeFactory,
    IConfiguration configuration)
{
    private string MainHelpMessage => $"""
        *TimeSheet Bot*

        *Tracking:* `/c` `/w` `/l` `/s` — commute, work, lunch, status
        *View & Edit:* `/li` `/e` `/d` — list, edit, delete
        *Reports:* `/r` [day|week|month|year|commute|daily|all]
        *Settings:* `/se` — timezone, reminders, alerts
        *Web Login:* `/login` — get login code for {configuration["FrontendUrl"] ?? "web interface"}
        *Other:* `/a` `/h` — about, help

        For details: `/help tracking`, `/help report`, `/help settings`
        """;

    private string MainHelpMessageAdmin => $"""
        *TimeSheet Bot*

        *Tracking:* `/c` `/w` `/l` `/s` — commute, work, lunch, status
        *View & Edit:* `/li` `/e` `/d` — list, edit, delete
        *Reports:* `/r` [day|week|month|year|commute|daily|all]
        *Settings:* `/se` — timezone, reminders, alerts
        *Web Login:* `/login` — get login code for {configuration["FrontendUrl"] ?? "web interface"}
        *Admin:* `/g` — generate registration mnemonic
        *Other:* `/a` `/h` — about, help

        For details: `/help tracking`, `/help report`, `/help settings`, `/help admin`
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

        *Text Reports:*
        `/report day` or `/r day` — Today's summary
        `/report week` or `/r week` — This week
        `/report month` or `/r month` — This month
        `/report year` or `/r year` — This year
        `/report commute` or `/r commute` — Commute patterns
        `/report daily` or `/r daily` — Daily averages (7/30/90 days)
        `/report table week` or `/r table week` — Daily table for week
        `/report table month` or `/r table month` — Daily table for month
        `/report all` or `/r all` — All reports as separate messages

        *Chart Reports:*
        `/report chart breakdown [period]` — Bar chart of daily work hours
        `/report chart trend [period]` — Line chart of work hours trend
        `/report chart activity [period]` — Stacked activity breakdown
        `/report chart averages` — Daily averages comparison (7/30/90 days)
        `/report chart commute` — Commute patterns by day of week

        *Chart periods:* week (default), month, year

        *Examples:*
        `/r chart breakdown` — This week's work hours
        `/r chart trend month` — This month's trend
        `/r chart activity week` — This week's activities
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

    private const string AdminHelpMessage = """
        *Admin Commands*

        `/generate` or `/g` — Generate registration mnemonic

        Generates a new BIP39 mnemonic for user registration. The mnemonic is single-use and will be consumed when a new user registers with it.

        *Usage:*
        1. Run `/generate` to create a new mnemonic
        2. Share the generated `/register [mnemonic]` command with the new user
        3. The new user runs the command to complete registration

        *Note:* This command is only available to administrators.
        """;

    /// <summary>
    /// Handles the /help command with optional subtopic argument.
    /// Shows context-aware help based on user role (admin vs regular user).
    /// </summary>
    public async Task HandleHelpAsync(
        ITelegramBotClient botClient,
        Message message,
        string[] commandParts,
        CancellationToken cancellationToken)
    {
        var userId = message.From?.Id;
        var username = message.From?.Username;

        logger.LogInformation(
            "Help command from user {UserId} ({Username})",
            userId ?? 0,
            username ?? "Unknown");

        // Check if the user is an admin
        var isAdmin = false;
        if (userId.HasValue)
        {
            isAdmin = await IsUserAdminAsync(userId.Value, cancellationToken);
        }

        var subtopic = commandParts.Length >= 2 ? commandParts[1].ToLowerInvariant() : null;

        var text = subtopic switch
        {
            "tracking" => TrackingHelpMessage,
            "report" => ReportHelpMessage,
            "settings" => SettingsHelpMessage,
            "admin" when isAdmin => AdminHelpMessage,
            "admin" => "⛔ Admin commands are only available to administrators.",
            _ => isAdmin ? MainHelpMessageAdmin : MainHelpMessage
        };

        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: text,
            parseMode: ParseMode.Markdown,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Checks if a user is an admin.
    /// </summary>
    private async Task<bool> IsUserAdminAsync(long telegramUserId, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var user = await userRepository.GetByTelegramUserIdAsync(telegramUserId, cancellationToken);
            return user?.IsAdmin ?? false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking admin status for user {UserId}", telegramUserId);
            return false;
        }
    }
}
