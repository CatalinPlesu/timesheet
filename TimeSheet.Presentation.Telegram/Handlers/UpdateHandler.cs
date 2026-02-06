using Telegram.Bot;
using Telegram.Bot.Types;

namespace TimeSheet.Presentation.Telegram.Handlers;

public class UpdateHandler(ILogger<UpdateHandler> logger)
{
    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        // Log the incoming update type
        var updateType = update.Type;
        logger.LogInformation("Received update of type: {UpdateType}", updateType);

        // For now, just log the message text if it's a text message
        if (update.Message?.Text is { } messageText)
        {
            logger.LogInformation(
                "Received message from {Username} ({UserId}): {MessageText}",
                update.Message.From?.Username ?? "Unknown",
                update.Message.From?.Id ?? 0,
                messageText);
        }

        // Command processing will be implemented in Epic 2 (Base Time Tracking)
    }

    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Error occurred while processing Telegram update");
        return Task.CompletedTask;
    }
}
