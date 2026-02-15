using TimeSheet.Core.Application.Interfaces.Persistence;
using TimeSheet.Core.Application.Interfaces.Services;
using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.Repositories;

namespace TimeSheet.Core.Application.Services;

/// <summary>
/// Service for handling user registration with BIP39 mnemonic authentication.
/// </summary>
public sealed class RegistrationService(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IMnemonicService mnemonicService) : IRegistrationService
{
    /// <inheritdoc/>
    public async Task<User?> RegisterUserAsync(
        long telegramUserId,
        string? telegramUsername,
        string mnemonicPhrase,
        int utcOffsetMinutes = 0,
        CancellationToken cancellationToken = default)
    {
        // Check if user already exists
        var existingUser = await userRepository.GetByTelegramUserIdAsync(telegramUserId, cancellationToken);
        if (existingUser != null)
        {
            return null; // User already registered
        }

        // Validate and consume the mnemonic (single-use)
        if (!await mnemonicService.ValidateAndConsumeMnemonicAsync(mnemonicPhrase, cancellationToken))
        {
            return null; // Invalid or already-used mnemonic
        }

        // Determine if this is the first user (becomes admin)
        var isFirstUser = !await userRepository.HasAnyUsersAsync(cancellationToken);

        // Create the new user
        var newUser = new User(
            telegramUserId: telegramUserId,
            telegramUsername: telegramUsername,
            isAdmin: isFirstUser,
            utcOffsetMinutes: utcOffsetMinutes);

        // Persist the user
        await userRepository.AddAsync(newUser, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);

        return newUser;
    }

    /// <inheritdoc/>
    public async Task<bool> HasAnyUsersAsync(CancellationToken cancellationToken = default)
    {
        return await userRepository.HasAnyUsersAsync(cancellationToken);
    }
}
