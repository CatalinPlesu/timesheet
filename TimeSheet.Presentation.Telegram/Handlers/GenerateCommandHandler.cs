using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TimeSheet.Core.Application.Interfaces.Services;
using TimeSheet.Core.Application.Interfaces.Persistence;
using TimeSheet.Core.Domain.Repositories;

namespace TimeSheet.Presentation.Telegram.Handlers;

/// <summary>
/// Handles the /generate command for admin users to pre-generate registration mnemonics.
/// </summary>
public class GenerateCommandHandler(
    ILogger<GenerateCommandHandler> logger,
    IServiceScopeFactory serviceScopeFactory)
{
    /// <summary>
    /// Handles the /generate command.
    /// Admin-only command that generates a new BIP39 mnemonic for user registration.
    /// </summary>
    public async Task HandleGenerateAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        var userId = message.From?.Id;
        var username = message.From?.Username;

        if (userId == null)
        {
            logger.LogWarning("Received /generate without user ID");
            return;
        }

        logger.LogInformation(
            "Generate command from user {UserId} ({Username})",
            userId.Value,
            username ?? "no username");

        try
        {
            // Create a scope to resolve scoped services
            using var scope = serviceScopeFactory.CreateScope();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var mnemonicService = scope.ServiceProvider.GetRequiredService<IMnemonicService>();

            // Check if the user is an admin
            var user = await userRepository.GetByTelegramUserIdAsync(userId.Value, cancellationToken);

            if (user == null)
            {
                // User is not registered - this shouldn't happen as auth is checked before routing
                logger.LogWarning(
                    "Non-registered user {UserId} attempted to use /generate",
                    userId.Value);
                return;
            }

            if (!user.IsAdmin)
            {
                // User is not an admin
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "⛔ This command is only available to administrators.",
                    cancellationToken: cancellationToken);

                logger.LogWarning(
                    "Non-admin user {UserId} ({Username}) attempted to use /generate",
                    userId.Value,
                    username ?? "no username");
                return;
            }

            // Generate a new mnemonic
            var mnemonic = mnemonicService.GenerateMnemonic();

            // Store it as pending
            mnemonicService.StorePendingMnemonic(mnemonic);

            // Get bot information to include the username
            var botInfo = await botClient.GetMe(cancellationToken);
            var botUsername = botInfo.Username ?? "timesheetbot";

            // Send first message: explanation of what the bot is
            var introMessage = $"""
                ✅ Generated new registration for TimeSheet bot.

                **What is TimeSheet?**
                A private time-tracking bot that helps you monitor your work hours, commute time, and lunch breaks. It's for your personal use - not employer surveillance.

                **Bot:** @{botUsername}
                """;

            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: introMessage,
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken);

            // Send second message: the registration command
            var registrationMessage = $"""
                `/register {mnemonic}`

                Share both messages with the new user. The mnemonic is single-use and will be consumed upon registration.
                """;

            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: registrationMessage,
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "Admin {UserId} ({Username}) generated a new registration mnemonic",
                userId.Value,
                username ?? "no username");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during /generate command for user {UserId}", userId.Value);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "❌ An error occurred while generating the mnemonic. Please try again.",
                cancellationToken: cancellationToken);
        }
    }
}
