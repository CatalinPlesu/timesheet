using Telegram.Bot;
using Telegram.Bot.Types;
using TimeSheet.Core.Application.Interfaces;

namespace TimeSheet.Presentation.Telegram.Handlers;

/// <summary>
/// Handles the /register command for user registration with BIP39 mnemonics.
/// </summary>
public class RegistrationCommandHandler(
    ILogger<RegistrationCommandHandler> logger,
    IServiceScopeFactory serviceScopeFactory)
{
    /// <summary>
    /// Handles the /register command.
    /// Expected format: /register [24-word mnemonic phrase]
    /// </summary>
    public async Task HandleRegisterAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        var userId = message.From?.Id;
        var username = message.From?.Username;

        if (userId == null)
        {
            logger.LogWarning("Received /register without user ID");
            return;
        }

        var messageText = message.Text ?? string.Empty;

        // Extract the mnemonic from the command
        // Format: /register word1 word2 ... word24
        var parts = messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Usage: /register [24-word mnemonic phrase]",
                cancellationToken: cancellationToken);
            return;
        }

        // Join all words after /register
        var mnemonicPhrase = string.Join(' ', parts[1..]);

        try
        {
            // Create a scope to resolve scoped services
            using var scope = serviceScopeFactory.CreateScope();
            var registrationService = scope.ServiceProvider.GetRequiredService<IRegistrationService>();

            // Attempt registration
            var newUser = await registrationService.RegisterUserAsync(
                telegramUserId: userId.Value,
                telegramUsername: username,
                mnemonicPhrase: mnemonicPhrase,
                utcOffsetMinutes: 0, // TODO: Prompt for timezone in Epic 5
                cancellationToken: cancellationToken);

            if (newUser == null)
            {
                // Registration failed (invalid mnemonic, already registered, etc.)
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "Registration failed. Invalid or expired mnemonic, or you are already registered.",
                    cancellationToken: cancellationToken);

                logger.LogWarning(
                    "Registration failed for Telegram user {UserId} ({Username})",
                    userId.Value,
                    username ?? "no username");
                return;
            }

            // Registration successful
            var welcomeMessage = newUser.IsAdmin
                ? "✅ Registration successful! You are the admin."
                : "✅ Registration successful!";

            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: welcomeMessage,
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "User registered: {UserId} ({Username}), IsAdmin: {IsAdmin}",
                newUser.TelegramUserId,
                newUser.TelegramUsername ?? "no username",
                newUser.IsAdmin);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during registration for user {UserId}", userId.Value);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "An error occurred during registration. Please try again.",
                cancellationToken: cancellationToken);
        }
    }
}
