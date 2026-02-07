namespace TimeSheet.Core.Application.Interfaces;

using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.Enums;

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
    /// Updates the auto-shutdown limit for a specific tracking state.
    /// </summary>
    /// <param name="telegramUserId">The Telegram user ID.</param>
    /// <param name="state">The tracking state to configure.</param>
    /// <param name="maxHours">The maximum allowed hours (null = no limit).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated user, or null if the user was not found.</returns>
    Task<User?> UpdateAutoShutdownLimitAsync(
        long telegramUserId,
        TrackingState state,
        decimal? maxHours,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by their Telegram user ID.
    /// </summary>
    /// <param name="telegramUserId">The Telegram user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user if found; otherwise, null.</returns>
    Task<User?> GetUserAsync(long telegramUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the lunch reminder hour setting for a user.
    /// </summary>
    /// <param name="telegramUserId">The Telegram user ID.</param>
    /// <param name="hour">The hour (0-23) at which to send a lunch reminder (null = disable reminder).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated user, or null if the user was not found.</returns>
    Task<User?> UpdateLunchReminderHourAsync(
        long telegramUserId,
        int? hour,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the target work hours per day setting for a user.
    /// </summary>
    /// <param name="telegramUserId">The Telegram user ID.</param>
    /// <param name="hours">The target work hours per day (null = disable target).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated user, or null if the user was not found.</returns>
    Task<User?> UpdateTargetWorkHoursAsync(
        long telegramUserId,
        decimal? hours,
        CancellationToken cancellationToken = default);
}
