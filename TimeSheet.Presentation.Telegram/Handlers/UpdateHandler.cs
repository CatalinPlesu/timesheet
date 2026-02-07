using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TimeSheet.Core.Application.Interfaces;
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
    DailyStatsCommandHandler dailyStatsCommandHandler,
    CommutePatternsCommandHandler commutePatternsCommandHandler,
    ReportCommandHandler reportCommandHandler,
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

        // /about and /help are available to everyone (registered or not)
        if (messageText.StartsWith("/about", StringComparison.OrdinalIgnoreCase))
        {
            await aboutCommandHandler.HandleAboutAsync(botClient, message, cancellationToken);
            return;
        }

        if (messageText.StartsWith("/help", StringComparison.OrdinalIgnoreCase))
        {
            await helpCommandHandler.HandleHelpAsync(botClient, message, cancellationToken);
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
        if (messageText.StartsWith("/commute", StringComparison.OrdinalIgnoreCase) ||
            messageText.StartsWith("/c ", StringComparison.OrdinalIgnoreCase) ||
            messageText == "/c")
        {
            await trackingCommandHandler.HandleCommuteAsync(botClient, message, cancellationToken);
        }
        else if (messageText.StartsWith("/work", StringComparison.OrdinalIgnoreCase) ||
                 messageText.StartsWith("/w ", StringComparison.OrdinalIgnoreCase) ||
                 messageText == "/w")
        {
            await trackingCommandHandler.HandleWorkAsync(botClient, message, cancellationToken);
        }
        else if (messageText.StartsWith("/lunch", StringComparison.OrdinalIgnoreCase) ||
                 messageText.StartsWith("/l ", StringComparison.OrdinalIgnoreCase) ||
                 messageText == "/l")
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
        else if (messageText.StartsWith("/dailystats", StringComparison.OrdinalIgnoreCase))
        {
            await dailyStatsCommandHandler.HandleDailyStatsAsync(botClient, message, cancellationToken);
        }
        else if (messageText.StartsWith("/commutepatterns", StringComparison.OrdinalIgnoreCase))
        {
            await commutePatternsCommandHandler.HandleCommutePatternsAsync(botClient, message, cancellationToken);
        }
        else if (messageText.StartsWith("/report", StringComparison.OrdinalIgnoreCase))
        {
            await reportCommandHandler.HandleReportAsync(botClient, message, cancellationToken);
        }
        else
        {
            logger.LogDebug("Unrecognized command: {MessageText}", messageText);
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
}
