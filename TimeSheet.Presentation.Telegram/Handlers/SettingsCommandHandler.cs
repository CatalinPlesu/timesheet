using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TimeSheet.Core.Application.Interfaces.Services;

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

        // Format lunch reminder for display
        var lunchReminderDisplay = user.LunchReminderHour.HasValue
            ? $"{user.LunchReminderHour.Value:D2}:{user.LunchReminderMinute:D2}"
            : "Not set";

        // Format target work hours for display
        var targetWorkHoursDisplay = user.TargetWorkHours.HasValue
            ? $"{user.TargetWorkHours.Value:F1} hours"
            : "Not set";

        // Format forgot-shutdown threshold for display
        var forgotShutdownDisplay = user.ForgotShutdownThresholdPercent.HasValue
            ? $"{user.ForgotShutdownThresholdPercent.Value}%"
            : "Not set";

        var settingsText = $"""
            ⚙️ *Settings*

            *Timezone:* {offsetDisplay}
            *Lunch Reminder:* {lunchReminderDisplay}
            *Target Work Hours:* {targetWorkHoursDisplay}
            *Forgot-Shutdown Alert:* {forgotShutdownDisplay}

            Use `/help settings` for usage examples.
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

    /// <summary>
    /// Handles lunch reminder configuration from /settings lunch command.
    /// </summary>
    public async Task HandleSettingsLunchAsync(
        ITelegramBotClient botClient,
        Message message,
        string[] commandParts,
        CancellationToken cancellationToken)
    {
        var userId = message.From?.Id;
        if (userId == null)
        {
            logger.LogWarning("Received /settings lunch without user ID");
            return;
        }

        // Parse lunch reminder time from command
        // Expected format: /settings lunch 12, /settings lunch 12:30, /settings lunch off
        if (commandParts.Length < 3)
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Usage: `/settings lunch [hour:minute]` or `/settings lunch off`\nExample: `/settings lunch 12:30`",
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken);
            return;
        }

        using var scope = serviceScopeFactory.CreateScope();
        var userSettingsService = scope.ServiceProvider.GetRequiredService<IUserSettingsService>();

        int? reminderHour = null;
        int reminderMinute = 0;
        var timeString = commandParts[2];

        // Check if user wants to disable the reminder
        if (timeString.Equals("off", StringComparison.OrdinalIgnoreCase))
        {
            reminderHour = null;
        }
        else
        {
            // Parse the time value (supports both "12" and "12:30" formats)
            if (timeString.Contains(':'))
            {
                var timeParts = timeString.Split(':');
                if (timeParts.Length != 2 ||
                    !int.TryParse(timeParts[0], out var hour) ||
                    !int.TryParse(timeParts[1], out var minute))
                {
                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: "Invalid time format. Please use HH:MM format (e.g., 12:30) or just hour (e.g., 12), or 'off' to disable.\nExample: `/settings lunch 12:30`",
                        parseMode: ParseMode.Markdown,
                        cancellationToken: cancellationToken);
                    return;
                }

                // Validate ranges
                if (hour < 0 || hour > 23)
                {
                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: "Hour must be between 0 and 23.",
                        cancellationToken: cancellationToken);
                    return;
                }

                if (minute < 0 || minute > 59)
                {
                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: "Minute must be between 0 and 59.",
                        cancellationToken: cancellationToken);
                    return;
                }

                reminderHour = hour;
                reminderMinute = minute;
            }
            else
            {
                // Parse just the hour (legacy format)
                if (!int.TryParse(timeString, out var hour))
                {
                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: "Invalid time. Please provide hour:minute (e.g., 12:30), just hour (e.g., 12), or 'off' to disable.\nExample: `/settings lunch 12:30`",
                        parseMode: ParseMode.Markdown,
                        cancellationToken: cancellationToken);
                    return;
                }

                // Validate range: 0 to 23
                if (hour < 0 || hour > 23)
                {
                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: "Hour must be between 0 and 23.",
                        cancellationToken: cancellationToken);
                    return;
                }

                reminderHour = hour;
                reminderMinute = 0;
            }
        }

        // Update the user's lunch reminder time
        var updatedUser = await userSettingsService.UpdateLunchReminderTimeAsync(
            userId.Value,
            reminderHour,
            reminderMinute,
            cancellationToken);

        if (updatedUser == null)
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Failed to update settings. Please try again.",
                cancellationToken: cancellationToken);
            return;
        }

        // Format response
        var responseText = reminderHour.HasValue
            ? $"✅ Lunch reminder set to {reminderHour.Value:D2}:{reminderMinute:D2}"
            : "✅ Lunch reminder disabled";

        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: responseText,
            cancellationToken: cancellationToken);

        logger.LogInformation(
            "User {UserId} updated lunch reminder time to {Time}",
            userId.Value,
            reminderHour.HasValue ? $"{reminderHour.Value:D2}:{reminderMinute:D2}" : "disabled");
    }

    /// <summary>
    /// Handles target work hours configuration from /settings target command.
    /// </summary>
    public async Task HandleSettingsTargetAsync(
        ITelegramBotClient botClient,
        Message message,
        string[] commandParts,
        CancellationToken cancellationToken)
    {
        var userId = message.From?.Id;
        if (userId == null)
        {
            logger.LogWarning("Received /settings target without user ID");
            return;
        }

        // Parse target work hours from command
        // Expected format: /settings target 8, /settings target 7.5, /settings target off
        if (commandParts.Length < 3)
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Usage: `/settings target [hours]` or `/settings target off`\nExample: `/settings target 8`",
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken);
            return;
        }

        using var scope = serviceScopeFactory.CreateScope();
        var userSettingsService = scope.ServiceProvider.GetRequiredService<IUserSettingsService>();

        decimal? targetHours = null;
        var hoursString = commandParts[2];

        // Check if user wants to disable the target
        if (hoursString.Equals("off", StringComparison.OrdinalIgnoreCase))
        {
            targetHours = null;
        }
        else
        {
            // Parse the hours value
            if (!decimal.TryParse(hoursString, out var hours))
            {
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "Invalid hours. Please provide a positive number, or 'off' to disable.\nExample: `/settings target 8`",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken);
                return;
            }

            // Validate range: must be positive
            if (hours <= 0)
            {
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "Target work hours must be a positive number.",
                    cancellationToken: cancellationToken);
                return;
            }

            targetHours = hours;
        }

        // Update the user's target work hours
        var updatedUser = await userSettingsService.UpdateTargetWorkHoursAsync(
            userId.Value,
            targetHours,
            cancellationToken);

        if (updatedUser == null)
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Failed to update settings. Please try again.",
                cancellationToken: cancellationToken);
            return;
        }

        // Format response
        var responseText = targetHours.HasValue
            ? $"✅ Target work hours set to {targetHours.Value:F1} hours per day"
            : "✅ Target work hours notification disabled";

        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: responseText,
            cancellationToken: cancellationToken);

        logger.LogInformation(
            "User {UserId} updated target work hours to {Hours}",
            userId.Value,
            targetHours?.ToString("F1") ?? "disabled");
    }

    /// <summary>
    /// Handles forgot-shutdown threshold configuration from /settings forgot command.
    /// </summary>
    public async Task HandleSettingsForgotAsync(
        ITelegramBotClient botClient,
        Message message,
        string[] commandParts,
        CancellationToken cancellationToken)
    {
        var userId = message.From?.Id;
        if (userId == null)
        {
            logger.LogWarning("Received /settings forgot without user ID");
            return;
        }

        // Parse forgot-shutdown threshold from command
        // Expected format: /settings forgot 150, /settings forgot 200, /settings forgot off
        if (commandParts.Length < 3)
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Usage: `/settings forgot [percent]` or `/settings forgot off`\nExample: `/settings forgot 150`",
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken);
            return;
        }

        using var scope = serviceScopeFactory.CreateScope();
        var userSettingsService = scope.ServiceProvider.GetRequiredService<IUserSettingsService>();

        int? thresholdPercent = null;
        var percentString = commandParts[2];

        // Check if user wants to disable the alert
        if (percentString.Equals("off", StringComparison.OrdinalIgnoreCase))
        {
            thresholdPercent = null;
        }
        else
        {
            // Parse the percent value
            if (!int.TryParse(percentString, out var percent))
            {
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "Invalid percentage. Please provide a number greater than 100, or 'off' to disable.\nExample: `/settings forgot 150`",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: cancellationToken);
                return;
            }

            // Validate range: must be > 100
            if (percent <= 100)
            {
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "Threshold percentage must be greater than 100.",
                    cancellationToken: cancellationToken);
                return;
            }

            thresholdPercent = percent;
        }

        // Update the user's forgot-shutdown threshold
        var updatedUser = await userSettingsService.UpdateForgotShutdownThresholdAsync(
            userId.Value,
            thresholdPercent,
            cancellationToken);

        if (updatedUser == null)
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Failed to update settings. Please try again.",
                cancellationToken: cancellationToken);
            return;
        }

        // Format response
        var responseText = thresholdPercent.HasValue
            ? $"✅ Forgot-shutdown alert set to {thresholdPercent.Value}% of average session duration"
            : "✅ Forgot-shutdown alert disabled";

        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: responseText,
            cancellationToken: cancellationToken);

        logger.LogInformation(
            "User {UserId} updated forgot-shutdown threshold to {Threshold}%",
            userId.Value,
            thresholdPercent?.ToString() ?? "disabled");
    }
}
