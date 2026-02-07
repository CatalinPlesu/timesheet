namespace TimeSheet.Core.Application.Interfaces;

using TimeSheet.Core.Domain.Entities;

/// <summary>
/// Service for checking and handling auto-shutdown of long-running tracking sessions.
/// </summary>
public interface IAutoShutdownService
{
    /// <summary>
    /// Checks all active sessions and auto-shuts down any that have exceeded their configured limits.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of sessions that were auto-shutdown.</returns>
    Task<List<TrackingSession>> CheckAndShutdownLongRunningSessionsAsync(CancellationToken cancellationToken = default);
}
