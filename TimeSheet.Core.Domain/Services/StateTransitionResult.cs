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
    public sealed class EndSession : StateTransitionResult
    {
        /// <summary>
        /// Gets the session that should be ended.
        /// </summary>
        public TrackingSession SessionToEnd { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EndSession"/> class.
        /// </summary>
        /// <param name="sessionToEnd">The session to end.</param>
        public EndSession(TrackingSession sessionToEnd)
        {
            SessionToEnd = sessionToEnd;
        }
    }

    /// <summary>
    /// Result indicating that a new session should be started, optionally ending the current one.
    /// </summary>
    public sealed class StartNewSession : StateTransitionResult
    {
        /// <summary>
        /// Gets the new session to start.
        /// </summary>
        public TrackingSession NewSession { get; }

        /// <summary>
        /// Gets the current session to end (null if no active session).
        /// </summary>
        public TrackingSession? SessionToEnd { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StartNewSession"/> class.
        /// </summary>
        /// <param name="newSession">The new session to start.</param>
        /// <param name="sessionToEnd">The current session to end (null if none).</param>
        public StartNewSession(TrackingSession newSession, TrackingSession? sessionToEnd = null)
        {
            NewSession = newSession;
            SessionToEnd = sessionToEnd;
        }
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
