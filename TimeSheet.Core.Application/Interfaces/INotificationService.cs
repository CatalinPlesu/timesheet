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
}
