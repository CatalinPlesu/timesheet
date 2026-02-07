namespace TimeSheet.Core.Application.Interfaces;

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
}
