using TimeSheet.Core.Application.Models;
using TimeSheet.Core.Domain.Enums;

namespace TimeSheet.Core.Application.Interfaces;

/// <summary>
/// Defines the contract for the time tracking application service.
/// Orchestrates state transitions and manages tracking sessions.
/// </summary>
public interface ITimeTrackingService
{
    /// <summary>
    /// Starts or toggles a tracking state for a user.
    /// </summary>
    /// <param name="userId">The Telegram user ID.</param>
    /// <param name="targetState">The state to start or toggle.</param>
    /// <param name="timestamp">The UTC timestamp for this state change.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A result indicating what happened:
    /// - SessionStarted: A new session was started.
    /// - SessionEnded: An active session was ended (toggle behavior).
    /// - NoChange: No active session and requesting same state again.
    /// </returns>
    Task<TrackingResult> StartStateAsync(
        long userId,
        TrackingState targetState,
        DateTime timestamp,
        CancellationToken cancellationToken = default);
}
