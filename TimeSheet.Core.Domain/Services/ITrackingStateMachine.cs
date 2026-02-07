using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.Enums;

namespace TimeSheet.Core.Domain.Services;

/// <summary>
/// Defines the contract for the tracking state machine.
/// Manages state transitions and enforces business rules for time tracking.
/// </summary>
public interface ITrackingStateMachine
{
    /// <summary>
    /// Starts a new tracking session for a user, applying toggle and exclusive state logic.
    /// </summary>
    /// <param name="userId">The Telegram user ID.</param>
    /// <param name="requestedState">The state to start or toggle.</param>
    /// <param name="timestamp">The UTC timestamp for this state change.</param>
    /// <param name="currentActiveSession">The current active session for the user (null if none).</param>
    /// <param name="lastCommuteDirection">The direction of the most recent commute session (null if none).</param>
    /// <param name="hasWorkedToday">Whether the user has any completed work sessions today.</param>
    /// <returns>
    /// A result indicating what action to take:
    /// - If toggle behavior applies (same state requested), returns EndSession with the session to end.
    /// - If a new state should start, returns StartNewSession with the new session and optionally the old session to end.
    /// </returns>
    StateTransitionResult ProcessStateChange(
        long userId,
        TrackingState requestedState,
        DateTime timestamp,
        TrackingSession? currentActiveSession,
        CommuteDirection? lastCommuteDirection,
        bool hasWorkedToday);
}
