using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using TimeSheet.Core.Application.Interfaces;
using TimeSheet.Presentation.Telegram.Handlers;

namespace TimeSheet.Presentation.Telegram;

public class Worker(
    ITelegramBotClient botClient,
    UpdateHandler updateHandler,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Get bot information
        var me = await botClient.GetMe(stoppingToken);
        logger.LogInformation("Telegram bot started: @{BotUsername} (ID: {BotId})", me.Username, me.Id);

        // Ensure database is created and migrated
        await EnsureDatabaseCreatedAsync(stoppingToken);

        // Check if any users exist; if not, generate a registration mnemonic
        await EnsureRegistrationMnemonicAsync(stoppingToken);

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

    /// <summary>
    /// Ensures the database is created and migrated.
    /// </summary>
    private async Task EnsureDatabaseCreatedAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TimeSheet.Infrastructure.Persistence.AppDbContext>();
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        logger.LogInformation("Database initialized");
    }

    /// <summary>
    /// Checks if any users exist. If not, generates a registration mnemonic and logs it.
    /// </summary>
    private async Task EnsureRegistrationMnemonicAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var registrationService = scope.ServiceProvider.GetRequiredService<IRegistrationService>();
        var mnemonicService = scope.ServiceProvider.GetRequiredService<IMnemonicService>();

        var hasUsers = await registrationService.HasAnyUsersAsync(cancellationToken);

        if (!hasUsers)
        {
            // Generate a new mnemonic for the first user (admin)
            var mnemonic = mnemonicService.GenerateMnemonic();
            mnemonicService.StorePendingMnemonic(mnemonic);

            // Log the registration command to console
            logger.LogWarning(
                "No users found. To register as admin, send this command in Telegram:\n\n" +
                "/register {Mnemonic}\n",
                mnemonic.ToString());
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Telegram bot is stopping...");
        await base.StopAsync(cancellationToken);
        logger.LogInformation("Telegram bot stopped gracefully");
    }
}
