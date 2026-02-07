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
    /// <param name="StartedSession">The newly started tracking session.</param>
    /// <param name="EndedSession">The session that was ended when starting the new one (null if none).</param>
    public sealed class SessionStarted(TrackingSession StartedSession, TrackingSession? EndedSession = null) : TrackingResult
    {
        /// <summary>
        /// Gets the newly started tracking session.
        /// </summary>
        public TrackingSession StartedSession { get; } = StartedSession;

        /// <summary>
        /// Gets the session that was ended when starting the new one (exclusive state behavior).
        /// Null if no session was active.
        /// </summary>
        public TrackingSession? EndedSession { get; } = EndedSession;
    }

    /// <summary>
    /// Result indicating that an active session was ended (toggle behavior).
    /// </summary>
    /// <param name="EndedSession">The session that was ended.</param>
    public sealed class SessionEnded(TrackingSession EndedSession) : TrackingResult
    {
        /// <summary>
        /// Gets the session that was ended.
        /// </summary>
        public TrackingSession EndedSession { get; } = EndedSession;
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
