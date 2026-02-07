using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.Enums;
using TimeSheet.Core.Domain.Interfaces;

namespace TimeSheet.Core.Application.Interfaces;

/// <summary>
/// Repository interface for TrackingSession entities with specialized query methods.
/// </summary>
public interface ITrackingSessionRepository : IRepository<TrackingSession>
{
    /// <summary>
    /// Gets the currently active tracking session for a user.
    /// </summary>
    /// <param name="userId">The Telegram user ID.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The active session if one exists; otherwise, null.</returns>
    Task<TrackingSession?> GetActiveSessionAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the most recent commute session for a user on a specific day.
    /// </summary>
    /// <param name="userId">The Telegram user ID.</param>
    /// <param name="date">The date to check (UTC).</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The most recent commute session if one exists; otherwise, null.</returns>
    Task<TrackingSession?> GetLastCommuteSessionAsync(long userId, DateTime date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the user has any completed work sessions on a specific day.
    /// </summary>
    /// <param name="userId">The Telegram user ID.</param>
    /// <param name="date">The date to check (UTC).</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>True if the user has worked today; otherwise, false.</returns>
    Task<bool> HasWorkedTodayAsync(long userId, DateTime date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the most recent tracking sessions for a user, ordered by start time descending.
    /// </summary>
    /// <param name="userId">The Telegram user ID.</param>
    /// <param name="count">The maximum number of sessions to retrieve.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A list of the most recent sessions.</returns>
    Task<List<TrackingSession>> GetRecentSessionsAsync(long userId, int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all tracking sessions for a user on a specific day, ordered chronologically.
    /// </summary>
    /// <param name="userId">The Telegram user ID.</param>
    /// <param name="date">The date to retrieve sessions for (UTC).</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A list of all sessions from the specified day.</returns>
    Task<List<TrackingSession>> GetSessionsByDateAsync(long userId, DateTime date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all currently active tracking sessions across all users.
    /// Used for checking if any sessions should be auto-shutdown.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A list of all active tracking sessions.</returns>
    Task<List<TrackingSession>> GetAllActiveSessionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the total work hours for a user on a specific day.
    /// Includes both completed and ongoing work sessions.
    /// </summary>
    /// <param name="userId">The Telegram user ID.</param>
    /// <param name="date">The date to calculate work hours for (UTC).</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The total work hours as a decimal (includes fractional hours).</returns>
    Task<decimal> GetTotalWorkHoursForDayAsync(long userId, DateTime date, CancellationToken cancellationToken = default);
}
