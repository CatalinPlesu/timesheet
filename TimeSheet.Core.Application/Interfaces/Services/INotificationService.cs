namespace TimeSheet.Core.Application.Interfaces.Services;

/// <summary>
/// Service for sending notifications to users via Telegram.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Sends a lunch reminder notification to a user.
    /// </summary>
    /// <param name="telegramUserId">The Telegram user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendLunchReminderAsync(long telegramUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification when the user reaches their target work hours.
    /// </summary>
    /// <param name="telegramUserId">The Telegram user ID.</param>
    /// <param name="targetHours">The target work hours that were reached.</param>
    /// <param name="actualHours">The actual work hours logged.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendWorkHoursCompleteAsync(
        long telegramUserId,
        decimal targetHours,
        decimal actualHours,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification when a session has been running unusually long.
    /// </summary>
    /// <param name="telegramUserId">The Telegram user ID.</param>
    /// <param name="state">The tracking state that's been running long.</param>
    /// <param name="currentDuration">The current duration in hours.</param>
    /// <param name="averageDuration">The average duration in hours.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendForgotShutdownReminderAsync(
        long telegramUserId,
        TimeSheet.Core.Domain.Enums.TrackingState state,
        decimal currentDuration,
        decimal averageDuration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification when a session has been automatically shut down.
    /// </summary>
    /// <param name="telegramUserId">The Telegram user ID.</param>
    /// <param name="state">The tracking state that was auto-shutdown.</param>
    /// <param name="duration">The duration of the session that was ended.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendAutoShutdownNotificationAsync(
        long telegramUserId,
        TimeSheet.Core.Domain.Enums.TrackingState state,
        TimeSpan duration,
        CancellationToken cancellationToken = default);
}
