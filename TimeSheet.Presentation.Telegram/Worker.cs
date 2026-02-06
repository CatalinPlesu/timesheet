using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using TimeSheet.Presentation.Telegram.Handlers;

namespace TimeSheet.Presentation.Telegram;

public class Worker(
    ITelegramBotClient botClient,
    UpdateHandler updateHandler,
    ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Get bot information
        var me = await botClient.GetMe(stoppingToken);
        logger.LogInformation("Telegram bot started: @{BotUsername} (ID: {BotId})", me.Username, me.Id);

        // Configure receiver options
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [UpdateType.Message], // Only receive message updates for now
            DropPendingUpdates = true // Discard pending updates on startup
        };

        // Start receiving updates
        await botClient.ReceiveAsync(
            updateHandler: updateHandler.HandleUpdateAsync,
            errorHandler: updateHandler.HandleErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Telegram bot is stopping...");
        await base.StopAsync(cancellationToken);
        logger.LogInformation("Telegram bot stopped gracefully");
    }
}
