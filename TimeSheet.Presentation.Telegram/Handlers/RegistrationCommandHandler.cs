using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TimeSheet.Core.Application.Interfaces;
using TimeSheet.Presentation.Telegram.Services;

namespace TimeSheet.Presentation.Telegram.Handlers;

/// <summary>
/// Handles the /register command for user registration with BIP39 mnemonics.
/// </summary>
public class RegistrationCommandHandler(
    ILogger<RegistrationCommandHandler> logger,
    IServiceScopeFactory serviceScopeFactory,
    RegistrationSessionStore sessionStore)
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
            var mnemonicService = scope.ServiceProvider.GetRequiredService<IMnemonicService>();

            // First, check if user is already registered
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var existingUser = await userRepository.GetByTelegramUserIdAsync(userId.Value, cancellationToken);
            if (existingUser != null)
            {
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "You are already registered.",
                    cancellationToken: cancellationToken);
                return;
            }

            // Validate the mnemonic (but don't consume it yet)
            if (!mnemonicService.ValidateMnemonic(mnemonicPhrase))
            {
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "Registration failed. Invalid or expired mnemonic.",
                    cancellationToken: cancellationToken);

                logger.LogWarning(
                    "Registration failed for Telegram user {UserId} ({Username}): invalid mnemonic",
                    userId.Value,
                    username ?? "no username");
                return;
            }

            // Consume the mnemonic now that it's validated
            if (!mnemonicService.ConsumeMnemonic(mnemonicPhrase))
            {
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "Registration failed. This mnemonic has already been used.",
                    cancellationToken: cancellationToken);

                logger.LogWarning(
                    "Registration failed for Telegram user {UserId} ({Username}): mnemonic already consumed",
                    userId.Value,
                    username ?? "no username");
                return;
            }

            // Mnemonic is valid - determine if this is the first user (admin)
            var isFirstUser = !await registrationService.HasAnyUsersAsync(cancellationToken);

            // Store pending registration session
            sessionStore.StorePendingRegistration(userId.Value, username, isFirstUser);

            // Prompt for UTC offset
            var promptMessage = """
                ✅ Mnemonic validated!

                Please provide your timezone UTC offset in hours.
                Examples:
                • `+2` for UTC+2 (e.g., Paris, Berlin)
                • `-5` for UTC-5 (e.g., New York)
                • `0` for UTC+0 (e.g., London)

                Valid range: -12 to +14
                """;

            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: promptMessage,
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "Mnemonic validated for user {UserId} ({Username}), awaiting UTC offset",
                userId.Value,
                username ?? "no username");
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

    /// <summary>
    /// Handles UTC offset input during registration.
    /// Called when a user with a pending registration sends a message.
    /// </summary>
    public async Task HandleUtcOffsetInputAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        var userId = message.From?.Id;
        if (userId == null)
        {
            logger.LogWarning("Received UTC offset input without user ID");
            return;
        }

        var messageText = message.Text?.Trim() ?? string.Empty;

        // Parse the UTC offset
        if (!int.TryParse(messageText, out var offsetHours))
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Invalid offset. Please provide a number between -12 and +14.\nExample: `+2` or `-5`",
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken);
            return;
        }

        // Validate range
        if (offsetHours < -12 || offsetHours > 14)
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "UTC offset must be between -12 and +14 hours.\nPlease try again.",
                cancellationToken: cancellationToken);
            return;
        }

        // Get the pending registration
        var pendingRegistration = sessionStore.GetAndRemovePendingRegistration(userId.Value);
        if (pendingRegistration == null)
        {
            logger.LogWarning(
                "No pending registration found for user {UserId}",
                userId.Value);
            return;
        }

        try
        {
            // Create a scope to resolve scoped services
            using var scope = serviceScopeFactory.CreateScope();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            // Convert hours to minutes
            var offsetMinutes = offsetHours * 60;

            // Create the user
            var newUser = new TimeSheet.Core.Domain.Entities.User(
                telegramUserId: pendingRegistration.TelegramUserId,
                telegramUsername: pendingRegistration.TelegramUsername,
                isAdmin: pendingRegistration.IsAdmin,
                utcOffsetMinutes: offsetMinutes);

            await userRepository.AddAsync(newUser, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);

            // Registration successful
            var offsetSign = offsetHours >= 0 ? "+" : "";
            var offsetDisplay = $"UTC{offsetSign}{offsetHours}";

            var welcomeMessage = newUser.IsAdmin
                ? $"✅ Registration successful! You are the admin.\n\nYour timezone is set to {offsetDisplay}.\nYou can change it anytime with `/settings`."
                : $"✅ Registration successful!\n\nYour timezone is set to {offsetDisplay}.\nYou can change it anytime with `/settings`.";

            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: welcomeMessage,
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "User registered: {UserId} ({Username}), IsAdmin: {IsAdmin}, UtcOffset: {UtcOffsetMinutes} minutes",
                newUser.TelegramUserId,
                newUser.TelegramUsername ?? "no username",
                newUser.IsAdmin,
                offsetMinutes);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error completing registration for user {UserId}", userId.Value);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "An error occurred during registration. Please try again with `/register`.",
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken);
        }
    }
}
