namespace TimeSheet.Core.Application.Interfaces;

using TimeSheet.Core.Domain.Entities;

/// <summary>
/// Service for managing user settings.
/// </summary>
public interface IUserSettingsService
{
    /// <summary>
    /// Updates a user's UTC offset.
    /// </summary>
    /// <param name="telegramUserId">The Telegram user ID.</param>
    /// <param name="utcOffsetMinutes">The new UTC offset in minutes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated user, or null if the user was not found.</returns>
    Task<User?> UpdateUtcOffsetAsync(
        long telegramUserId,
        int utcOffsetMinutes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by their Telegram user ID.
    /// </summary>
    /// <param name="telegramUserId">The Telegram user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user if found; otherwise, null.</returns>
    Task<User?> GetUserAsync(long telegramUserId, CancellationToken cancellationToken = default);
}
