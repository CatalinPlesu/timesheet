using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TimeSheet.Presentation.Telegram.Handlers;

public class UpdateHandler(
    ILogger<UpdateHandler> logger,
    TrackingCommandHandler trackingCommandHandler)
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

        logger.LogInformation(
            "Received message from {Username} ({UserId}): {MessageText}",
            message.From?.Username ?? "Unknown",
            message.From?.Id ?? 0,
            messageText);

        // Route commands
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
