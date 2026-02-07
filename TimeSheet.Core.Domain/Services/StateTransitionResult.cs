using TimeSheet.Core.Domain.Entities;

namespace TimeSheet.Core.Domain.Services;

/// <summary>
/// Represents the result of a state transition request.
/// </summary>
public abstract class StateTransitionResult
{
    /// <summary>
    /// Result indicating that the current active session should be ended (toggle behavior).
    /// </summary>
    /// <param name="SessionToEnd">The session that should be ended.</param>
    public sealed class EndSession(TrackingSession SessionToEnd) : StateTransitionResult
    {
        /// <summary>
        /// Gets the session that should be ended.
        /// </summary>
        public TrackingSession SessionToEnd { get; } = SessionToEnd;
    }

    /// <summary>
    /// Result indicating that a new session should be started, optionally ending the current one.
    /// </summary>
    /// <param name="NewSession">The new session to start.</param>
    /// <param name="SessionToEnd">The current session to end (null if no active session).</param>
    public sealed class StartNewSession(TrackingSession NewSession, TrackingSession? SessionToEnd = null) : StateTransitionResult
    {
        /// <summary>
        /// Gets the new session to start.
        /// </summary>
        public TrackingSession NewSession { get; } = NewSession;

        /// <summary>
        /// Gets the current session to end (null if no active session).
        /// </summary>
        public TrackingSession? SessionToEnd { get; } = SessionToEnd;
    }

    /// <summary>
    /// Result indicating no action should be taken (user is idle and requested idle).
    /// </summary>
    public sealed class NoChange : StateTransitionResult
    {
        /// <summary>
        /// Gets the singleton instance of <see cref="NoChange"/>.
        /// </summary>
        public static readonly NoChange Instance = new();

        private NoChange() { }
    }
}
