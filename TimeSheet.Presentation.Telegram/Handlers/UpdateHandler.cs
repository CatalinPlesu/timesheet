using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TimeSheet.Core.Application.Interfaces.Services;
using TimeSheet.Core.Application.Interfaces.Persistence;
using TimeSheet.Core.Domain.Repositories;
using TimeSheet.Presentation.Telegram.Services;

namespace TimeSheet.Presentation.Telegram.Handlers;

public class UpdateHandler(
    ILogger<UpdateHandler> logger,
    IServiceScopeFactory serviceScopeFactory,
    TrackingCommandHandler trackingCommandHandler,
    RegistrationCommandHandler registrationCommandHandler,
    AboutCommandHandler aboutCommandHandler,
    HelpCommandHandler helpCommandHandler,
    EditCommandHandler editCommandHandler,
    DeleteCommandHandler deleteCommandHandler,
    GenerateCommandHandler generateCommandHandler,
    ListCommandHandler listCommandHandler,
    SettingsCommandHandler settingsCommandHandler,
    ReportCommandHandler reportCommandHandler,
    StatusCommandHandler statusCommandHandler,
    LoginCommandHandler loginCommandHandler,
    RegistrationSessionStore registrationSessionStore)
{
    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        // Log the incoming update type
        var updateType = update.Type;
        logger.LogInformation("Received update of type: {UpdateType}", updateType);

        // Handle different update types
        var handler = update.Type switch
        {
            UpdateType.Message => HandleMessageAsync(botClient, update.Message!, cancellationToken),
            UpdateType.CallbackQuery => HandleCallbackQueryAsync(botClient, update.CallbackQuery!, cancellationToken),
            _ => HandleUnknownUpdateAsync(update)
        };

        await handler;
    }

    private async Task HandleMessageAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var messageText = message.Text;
        if (string.IsNullOrWhiteSpace(messageText))
        {
            logger.LogDebug("Received non-text message");
            return;
        }

        var userId = message.From?.Id;
        var username = message.From?.Username;

        logger.LogInformation(
            "Received message from {Username} ({UserId}): {MessageText}",
            username ?? "Unknown",
            userId ?? 0,
            messageText);

        // Expand aliases to full commands
        messageText = ExpandAliases(messageText);

        // /about and /help are available to everyone (registered or not)
        if (messageText.StartsWith("/about", StringComparison.OrdinalIgnoreCase))
        {
            await aboutCommandHandler.HandleAboutAsync(botClient, message, cancellationToken);
            return;
        }

        if (messageText.StartsWith("/help", StringComparison.OrdinalIgnoreCase))
        {
            var helpParts = messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            await helpCommandHandler.HandleHelpAsync(botClient, message, helpParts, cancellationToken);
            return;
        }

        // /register is available to everyone (but requires valid mnemonic)
        if (messageText.StartsWith("/register", StringComparison.OrdinalIgnoreCase))
        {
            await registrationCommandHandler.HandleRegisterAsync(botClient, message, cancellationToken);
            return;
        }

        // Check if user has a pending registration (awaiting UTC offset input)
        if (userId.HasValue && registrationSessionStore.HasPendingRegistration(userId.Value))
        {
            await registrationCommandHandler.HandleUtcOffsetInputAsync(botClient, message, cancellationToken);
            return;
        }

        // All other commands require authentication
        if (userId == null || !await IsUserRegisteredAsync(userId.Value, cancellationToken))
        {
            logger.LogWarning(
                "Ignoring command from non-registered user {UserId} ({Username}): {MessageText}",
                userId ?? 0,
                username ?? "Unknown",
                messageText);
            return; // Silently ignore
        }

        // Route commands for registered users
        if (messageText.StartsWith("/commute", StringComparison.OrdinalIgnoreCase))
        {
            await trackingCommandHandler.HandleCommuteAsync(botClient, message, cancellationToken);
        }
        else if (messageText.StartsWith("/work", StringComparison.OrdinalIgnoreCase))
        {
            await trackingCommandHandler.HandleWorkAsync(botClient, message, cancellationToken);
        }
        else if (messageText.StartsWith("/lunch", StringComparison.OrdinalIgnoreCase))
        {
            await trackingCommandHandler.HandleLunchAsync(botClient, message, cancellationToken);
        }
        else if (messageText.StartsWith("/edit", StringComparison.OrdinalIgnoreCase))
        {
            await editCommandHandler.HandleEditAsync(botClient, message, cancellationToken);
        }
        else if (messageText.StartsWith("/delete", StringComparison.OrdinalIgnoreCase))
        {
            await deleteCommandHandler.HandleDeleteAsync(botClient, message, cancellationToken);
        }
        else if (messageText.StartsWith("/generate", StringComparison.OrdinalIgnoreCase))
        {
            await generateCommandHandler.HandleGenerateAsync(botClient, message, cancellationToken);
        }
        else if (messageText.StartsWith("/list", StringComparison.OrdinalIgnoreCase))
        {
            await listCommandHandler.HandleListAsync(botClient, message, cancellationToken);
        }
        else if (messageText.StartsWith("/settings", StringComparison.OrdinalIgnoreCase))
        {
            // Parse the command to see if it's a settings update
            var parts = messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2 && parts[1].Equals("utc", StringComparison.OrdinalIgnoreCase))
            {
                await settingsCommandHandler.HandleSettingsUtcAsync(botClient, message, parts, cancellationToken);
            }
            else if (parts.Length >= 2 && parts[1].Equals("lunch", StringComparison.OrdinalIgnoreCase))
            {
                await settingsCommandHandler.HandleSettingsLunchAsync(botClient, message, parts, cancellationToken);
            }
            else if (parts.Length >= 2 && parts[1].Equals("target", StringComparison.OrdinalIgnoreCase))
            {
                await settingsCommandHandler.HandleSettingsTargetAsync(botClient, message, parts, cancellationToken);
            }
            else if (parts.Length >= 2 && parts[1].Equals("forgot", StringComparison.OrdinalIgnoreCase))
            {
                await settingsCommandHandler.HandleSettingsForgotAsync(botClient, message, parts, cancellationToken);
            }
            else
            {
                await settingsCommandHandler.HandleSettingsAsync(botClient, message, cancellationToken);
            }
        }
        else if (messageText.StartsWith("/report", StringComparison.OrdinalIgnoreCase))
        {
            await reportCommandHandler.HandleReportAsync(botClient, message, cancellationToken, expandedText: messageText);
        }
        else if (messageText.StartsWith("/status", StringComparison.OrdinalIgnoreCase))
        {
            await statusCommandHandler.HandleStatusAsync(botClient, message, cancellationToken);
        }
        else if (messageText.StartsWith("/login", StringComparison.OrdinalIgnoreCase))
        {
            await loginCommandHandler.HandleLoginAsync(botClient, message, cancellationToken);
        }
        else
        {
            logger.LogDebug("Unrecognized command: {MessageText}", messageText);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Unknown command. Try /help for available commands.",
                cancellationToken: cancellationToken);
        }
    }

    private async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var userId = callbackQuery.From.Id;

        logger.LogInformation(
            "Received callback query from {UserId}: {Data}",
            userId,
            callbackQuery.Data);

        // Check if user is registered
        if (!await IsUserRegisteredAsync(userId, cancellationToken))
        {
            logger.LogWarning(
                "Ignoring callback query from non-registered user {UserId}",
                userId);
            await botClient.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: "You need to register first",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        // Route callback queries to appropriate handlers
        var data = callbackQuery.Data ?? string.Empty;

        if (data.StartsWith("edit:", StringComparison.OrdinalIgnoreCase))
        {
            await editCommandHandler.HandleCallbackQueryAsync(botClient, callbackQuery, cancellationToken);
        }
        else if (data.StartsWith("delete:", StringComparison.OrdinalIgnoreCase))
        {
            await deleteCommandHandler.HandleCallbackQueryAsync(botClient, callbackQuery, cancellationToken);
        }
        else if (data.StartsWith("status:", StringComparison.OrdinalIgnoreCase))
        {
            await statusCommandHandler.HandleCallbackQueryAsync(botClient, callbackQuery, cancellationToken);
        }
        else if (data.StartsWith("track:", StringComparison.OrdinalIgnoreCase))
        {
            await trackingCommandHandler.HandleCallbackQueryAsync(botClient, callbackQuery, cancellationToken);
        }
        else
        {
            logger.LogDebug("Unrecognized callback query data: {Data}", data);
            await botClient.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Checks if a user is registered in the system.
    /// </summary>
    private async Task<bool> IsUserRegisteredAsync(long telegramUserId, CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var user = await userRepository.GetByTelegramUserIdAsync(telegramUserId, cancellationToken);
        return user != null;
    }

    private Task HandleUnknownUpdateAsync(Update update)
    {
        logger.LogDebug("Received update of unhandled type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }

    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Error occurred while processing Telegram update");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Expands command aliases to their full forms at all levels.
    /// For example: "/h r" becomes "/help report", "/c" becomes "/commute"
    /// </summary>
    private static string ExpandAliases(string input)
    {
        if (string.IsNullOrWhiteSpace(input) || !input.StartsWith('/'))
        {
            return input;
        }

        // Split the command into parts
        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return input;
        }

        // Define command-level alias mappings
        var commandAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "/a", "/about" },
            { "/h", "/help" },
            { "/re", "/register" },
            { "/c", "/commute" },
            { "/w", "/work" },
            { "/l", "/lunch" },
            { "/e", "/edit" },
            { "/d", "/delete" },
            { "/g", "/generate" },
            { "/li", "/list" },
            { "/se", "/settings" },
            { "/r", "/report" },
            { "/s", "/status" },
            { "/lo", "/login" }
        };

        // Expand the command (first part)
        if (commandAliases.TryGetValue(parts[0], out var expandedCommand))
        {
            parts[0] = expandedCommand;
        }

        // Expand the subcommand (second part, if present) - context-aware based on parent command
        if (parts.Length >= 2 && parts[1].Length == 1)
        {
            var subcommand = parts[1];
            var parentCommand = parts[0].TrimStart('/');

            // Define subcommand aliases per parent command
            var expandedSubcommand = (parentCommand.ToLowerInvariant(), subcommand.ToLowerInvariant()) switch
            {
                // /help subcommands
                ("help", "t") => "tracking",
                ("help", "r") => "report",
                ("help", "s") => "settings",

                // /report subcommands
                ("report", "d") => "day",
                ("report", "w") => "week",
                ("report", "m") => "month",
                ("report", "y") => "year",
                ("report", "c") => "commute",
                ("report", "a") => "all",
                ("report", "t") => "table",

                // /settings subcommands
                ("settings", "u") => "utc",
                ("settings", "l") => "lunch",
                ("settings", "t") => "target",
                ("settings", "f") => "forgot",

                // No match - keep original
                _ => null
            };

            if (expandedSubcommand != null)
            {
                parts[1] = expandedSubcommand;
            }
        }

        // Expand the third level (e.g., /report table w -> /report table week)
        if (parts.Length >= 3 && parts[2].Length == 1)
        {
            var parentCommand = parts[0].TrimStart('/');
            var subcommand = parts[1];
            var thirdLevel = parts[2];

            var expandedThirdLevel = (parentCommand.ToLowerInvariant(), subcommand.ToLowerInvariant(), thirdLevel.ToLowerInvariant()) switch
            {
                // /report table subcommands
                ("report", "table", "w") => "week",
                ("report", "table", "m") => "month",
                ("report", "table", "y") => "year",

                // No match - keep original
                _ => null
            };

            if (expandedThirdLevel != null)
            {
                parts[2] = expandedThirdLevel;
            }
        }

        // Reconstruct the command
        return string.Join(' ', parts);
    }
}
