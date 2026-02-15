using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TimeSheet.Core.Application.Interfaces.Services;
using TimeSheet.Core.Domain.Repositories;

namespace TimeSheet.Presentation.Telegram.Handlers;

/// <summary>
/// Handles the /login command for registered users to generate one-time OTP mnemonics.
/// </summary>
public class LoginCommandHandler(
    ILogger<LoginCommandHandler> logger,
    IServiceScopeFactory serviceScopeFactory,
    IConfiguration configuration)
{
    /// <summary>
    /// Handles the /login command.
    /// Generates a new BIP39 mnemonic that can be used as a one-time password for web login.
    /// </summary>
    public async Task HandleLoginAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        var userId = message.From?.Id;
        var username = message.From?.Username;

        if (userId == null)
        {
            logger.LogWarning("Received /login without user ID");
            return;
        }

        logger.LogInformation(
            "Login command from user {UserId} ({Username})",
            userId.Value,
            username ?? "no username");

        try
        {
            // Create a scope to resolve scoped services
            using var scope = serviceScopeFactory.CreateScope();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var mnemonicService = scope.ServiceProvider.GetRequiredService<IMnemonicService>();

            // Check if the user is registered
            var user = await userRepository.GetByTelegramUserIdAsync(userId.Value, cancellationToken);

            if (user == null)
            {
                // User is not registered
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "You are not registered. Please contact admin.",
                    cancellationToken: cancellationToken);

                logger.LogWarning(
                    "Non-registered user {UserId} ({Username}) attempted to use /login",
                    userId.Value,
                    username ?? "no username");
                return;
            }

            // Generate a new mnemonic
            var mnemonic = mnemonicService.GenerateMnemonic();

            // Store it as pending
            await mnemonicService.StorePendingMnemonicAsync(mnemonic, cancellationToken);

            // Send the login code to the user
            var frontendUrl = configuration["FrontendUrl"] ?? "the web interface";
            var loginMessage = $"""
                🔑 Your login code:

                `{mnemonic}`

                This code is valid for one use only. Use it to log into the web interface at:
                {frontendUrl}
                """;

            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: loginMessage,
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "User {UserId} ({Username}) generated a login mnemonic",
                userId.Value,
                username ?? "no username");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during /login command for user {UserId}", userId.Value);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Failed to generate login code. Please try again.",
                cancellationToken: cancellationToken);
        }
    }
}
