namespace TimeSheet.Core.Application.Interfaces.Services;

using TimeSheet.Core.Domain.Entities;

/// <summary>
/// Service for handling user registration with BIP39 mnemonic authentication.
/// </summary>
public interface IRegistrationService
{
    /// <summary>
    /// Registers a new user with the provided mnemonic and Telegram details.
    /// </summary>
    /// <param name="telegramUserId">The Telegram user ID.</param>
    /// <param name="telegramUsername">The Telegram username (optional).</param>
    /// <param name="mnemonicPhrase">The 24-word BIP39 mnemonic phrase.</param>
    /// <param name="utcOffsetMinutes">The user's UTC offset in minutes (default: 0).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly created user, or null if registration failed (invalid/used mnemonic, user already exists).</returns>
    Task<User?> RegisterUserAsync(
        long telegramUserId,
        string? telegramUsername,
        string mnemonicPhrase,
        int utcOffsetMinutes = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any users exist in the system.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if at least one user exists; otherwise, false.</returns>
    Task<bool> HasAnyUsersAsync(CancellationToken cancellationToken = default);
}
