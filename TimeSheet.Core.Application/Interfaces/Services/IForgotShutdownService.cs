namespace TimeSheet.Core.Application.Interfaces.Services;

using TimeSheet.Core.Domain.Entities;

/// <summary>
/// Service for detecting and notifying users about long-running sessions that may have been forgotten.
/// </summary>
public interface IForgotShutdownService
{
    /// <summary>
    /// Checks all active sessions and sends reminders for any that have exceeded
    /// the user's configured threshold based on their historical averages.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of sessions that were detected as potentially forgotten.</returns>
    Task<List<TrackingSession>> CheckAndNotifyLongRunningSessionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a specific session should trigger a forgot-shutdown reminder.
    /// </summary>
    /// <param name="session">The session to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if a reminder should be sent; otherwise, false.</returns>
    Task<bool> ShouldNotifyForSessionAsync(TrackingSession session, CancellationToken cancellationToken = default);
}
