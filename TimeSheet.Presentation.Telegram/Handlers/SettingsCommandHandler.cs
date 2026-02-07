using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TimeSheet.Core.Application.Interfaces;

namespace TimeSheet.Presentation.Telegram.Handlers;

/// <summary>
/// Handles the /settings command for viewing and updating user settings.
/// </summary>
public class SettingsCommandHandler(
    ILogger<SettingsCommandHandler> logger,
    IServiceScopeFactory serviceScopeFactory)
{
    /// <summary>
    /// Handles the /settings command.
    /// Shows current settings and provides options to change them.
    /// </summary>
    public async Task HandleSettingsAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        var userId = message.From?.Id;
        if (userId == null)
        {
            logger.LogWarning("Received /settings without user ID");
            return;
        }

        logger.LogInformation(
            "Settings command from user {UserId} ({Username})",
            userId.Value,
            message.From?.Username ?? "Unknown");

        using var scope = serviceScopeFactory.CreateScope();
        var userSettingsService = scope.ServiceProvider.GetRequiredService<IUserSettingsService>();

        // Get the user
        var user = await userSettingsService.GetUserAsync(userId.Value, cancellationToken);
        if (user == null)
        {
            logger.LogWarning("User {UserId} not found", userId.Value);
            return;
        }

        // Format UTC offset for display
        var offsetHours = user.UtcOffsetMinutes / 60;
        var offsetMinutes = Math.Abs(user.UtcOffsetMinutes % 60);
        var offsetSign = user.UtcOffsetMinutes >= 0 ? "+" : "-";
        var offsetDisplay = offsetMinutes > 0
            ? $"UTC{offsetSign}{Math.Abs(offsetHours)}:{offsetMinutes:D2}"
            : $"UTC{offsetSign}{Math.Abs(offsetHours)}";

        var settingsText = $"""
            ⚙️ *Settings*

            *Timezone:* {offsetDisplay}

            To change your timezone, use:
            `/settings utc [±hours]`

            Examples:
            • `/settings utc +2` — Set to UTC+2
            • `/settings utc -5` — Set to UTC-5
            • `/settings utc 0` — Set to UTC+0
            """;

        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: settingsText,
            parseMode: ParseMode.Markdown,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Handles UTC offset update from /settings utc command.
    /// </summary>
    public async Task HandleSettingsUtcAsync(
        ITelegramBotClient botClient,
        Message message,
        string[] commandParts,
        CancellationToken cancellationToken)
    {
        var userId = message.From?.Id;
        if (userId == null)
        {
            logger.LogWarning("Received /settings utc without user ID");
            return;
        }

        // Parse UTC offset from command
        // Expected format: /settings utc +2, /settings utc -5, /settings utc 0
        if (commandParts.Length < 3)
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Usage: `/settings utc [±hours]`\nExample: `/settings utc +2`",
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken);
            return;
        }

        var offsetString = commandParts[2];
        if (!int.TryParse(offsetString, out var offsetHours))
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Invalid offset. Please provide a number between -12 and +14.\nExample: `/settings utc +2`",
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken);
            return;
        }

        // Validate range: -12 to +14
        if (offsetHours < -12 || offsetHours > 14)
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "UTC offset must be between -12 and +14 hours.",
                cancellationToken: cancellationToken);
            return;
        }

        using var scope = serviceScopeFactory.CreateScope();
        var userSettingsService = scope.ServiceProvider.GetRequiredService<IUserSettingsService>();

        // Convert hours to minutes
        var offsetMinutes = offsetHours * 60;

        // Update the user's UTC offset
        var updatedUser = await userSettingsService.UpdateUtcOffsetAsync(
            userId.Value,
            offsetMinutes,
            cancellationToken);

        if (updatedUser == null)
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Failed to update settings. Please try again.",
                cancellationToken: cancellationToken);
            return;
        }

        // Format display
        var offsetSign = offsetHours >= 0 ? "+" : "";
        var offsetDisplay = $"UTC{offsetSign}{offsetHours}";

        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: $"✅ Timezone updated to {offsetDisplay}",
            cancellationToken: cancellationToken);

        logger.LogInformation(
            "User {UserId} updated UTC offset to {OffsetMinutes} minutes",
            userId.Value,
            offsetMinutes);
    }
}
