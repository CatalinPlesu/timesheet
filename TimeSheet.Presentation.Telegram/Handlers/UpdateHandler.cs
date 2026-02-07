using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TimeSheet.Core.Application.Interfaces;

namespace TimeSheet.Presentation.Telegram.Handlers;

public class UpdateHandler(
    ILogger<UpdateHandler> logger,
    IServiceScopeFactory serviceScopeFactory,
    TrackingCommandHandler trackingCommandHandler,
    RegistrationCommandHandler registrationCommandHandler,
    AboutCommandHandler aboutCommandHandler,
    HelpCommandHandler helpCommandHandler)
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
        else
        {
            logger.LogDebug("Unrecognized command: {MessageText}", messageText);
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
