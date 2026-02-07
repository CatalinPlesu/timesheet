using TimeSheet.Core.Domain.Entities;

namespace TimeSheet.Core.Application.Models;

/// <summary>
/// Represents the result of a time tracking operation.
/// This is a discriminated union-style result type.
/// </summary>
public abstract class TrackingResult
{
    /// <summary>
    /// Result indicating that a new tracking session was started.
    /// May include information about a previous session that was ended.
    /// </summary>
    public sealed class SessionStarted : TrackingResult
    {
        /// <summary>
        /// Gets the newly started tracking session.
        /// </summary>
        public TrackingSession StartedSession { get; }

        /// <summary>
        /// Gets the session that was ended when starting the new one (exclusive state behavior).
        /// Null if no session was active.
        /// </summary>
        public TrackingSession? EndedSession { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionStarted"/> class.
        /// </summary>
        /// <param name="startedSession">The newly started session.</param>
        /// <param name="endedSession">The session that was ended (null if none).</param>
        public SessionStarted(TrackingSession startedSession, TrackingSession? endedSession = null)
        {
            StartedSession = startedSession;
            EndedSession = endedSession;
        }
    }

    /// <summary>
    /// Result indicating that an active session was ended (toggle behavior).
    /// </summary>
    public sealed class SessionEnded : TrackingResult
    {
        /// <summary>
        /// Gets the session that was ended.
        /// </summary>
        public TrackingSession EndedSession { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionEnded"/> class.
        /// </summary>
        /// <param name="endedSession">The session that was ended.</param>
        public SessionEnded(TrackingSession endedSession)
        {
            EndedSession = endedSession;
        }
    }

    /// <summary>
    /// Result indicating no change occurred.
    /// This can happen if the user is idle and requests an idle state again.
    /// </summary>
    public sealed class NoChange : TrackingResult
    {
        /// <summary>
        /// Gets the singleton instance of <see cref="NoChange"/>.
        /// </summary>
        public static readonly NoChange Instance = new();

        private NoChange() { }
    }
}
