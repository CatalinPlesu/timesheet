using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.Enums;

namespace TimeSheet.Core.Domain.Services;

/// <summary>
/// Implements the tracking state machine, managing state transitions and enforcing business rules.
/// </summary>
public class TrackingStateMachine : ITrackingStateMachine
{
    /// <summary>
    /// Processes a state change request, applying toggle and exclusive state logic.
    /// </summary>
    /// <param name="userId">The Telegram user ID.</param>
    /// <param name="requestedState">The state to start or toggle.</param>
    /// <param name="timestamp">The UTC timestamp for this state change.</param>
    /// <param name="currentActiveSession">The current active session for the user (null if none).</param>
    /// <param name="lastCommuteDirection">The direction of the most recent commute session (null if none).</param>
    /// <param name="hasWorkedToday">Whether the user has any completed work sessions today.</param>
    /// <returns>A result indicating what action to take.</returns>
    /// <exception cref="ArgumentException">Thrown when requestedState is Idle.</exception>
    public StateTransitionResult ProcessStateChange(
        long userId,
        TrackingState requestedState,
        DateTime timestamp,
        TrackingSession? currentActiveSession,
        CommuteDirection? lastCommuteDirection,
        bool hasWorkedToday)
    {
        if (requestedState == TrackingState.Idle)
            throw new ArgumentException("Cannot explicitly request Idle state. Use toggle behavior to end current state.", nameof(requestedState));

        // If no active session, start the requested state
        if (currentActiveSession == null)
        {
            var newSession = CreateSession(userId, requestedState, timestamp, lastCommuteDirection, hasWorkedToday);
            return new StateTransitionResult.StartNewSession(newSession);
        }

        // Toggle behavior: if requesting the same state, end the current session
        if (currentActiveSession.State == requestedState)
        {
            return new StateTransitionResult.EndSession(currentActiveSession);
        }

        // Exclusive state behavior: end current session and start new one
        var nextSession = CreateSession(userId, requestedState, timestamp, lastCommuteDirection, hasWorkedToday);
        return new StateTransitionResult.StartNewSession(nextSession, currentActiveSession);
    }

    /// <summary>
    /// Creates a new tracking session with the appropriate commute direction if applicable.
    /// </summary>
    private TrackingSession CreateSession(
        long userId,
        TrackingState state,
        DateTime timestamp,
        CommuteDirection? lastCommuteDirection,
        bool hasWorkedToday)
    {
        CommuteDirection? commuteDirection = null;

        if (state == TrackingState.Commuting)
        {
            commuteDirection = DetermineCommuteDirection(lastCommuteDirection, hasWorkedToday);
        }

        return new TrackingSession(userId, state, timestamp, commuteDirection);
    }

    /// <summary>
    /// Determines the direction of a new commute based on context.
    /// Logic:
    /// - First commute of the day (no previous commute) = ToWork
    /// - After working (hasWorkedToday = true) = ToHome
    /// - Second commute without working yet = ToHome (user went home without working)
    /// </summary>
    private CommuteDirection DetermineCommuteDirection(
        CommuteDirection? lastCommuteDirection,
        bool hasWorkedToday)
    {
        // First commute of the day
        if (lastCommuteDirection == null)
            return CommuteDirection.ToWork;

        // User has worked today, so commute must be going home
        if (hasWorkedToday)
            return CommuteDirection.ToHome;

        // User had a previous commute but hasn't worked yet.
        // Alternate the direction (likely they went home without working).
        return lastCommuteDirection == CommuteDirection.ToWork
            ? CommuteDirection.ToHome
            : CommuteDirection.ToWork;
    }
}
