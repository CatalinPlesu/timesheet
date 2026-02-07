namespace TimeSheet.Core.Application.Services;

using TimeSheet.Core.Application.Interfaces;
using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.Enums;

/// <summary>
/// Service for checking and handling auto-shutdown of long-running tracking sessions.
/// </summary>
public sealed class AutoShutdownService(
    ITrackingSessionRepository trackingSessionRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork) : IAutoShutdownService
{
    /// <inheritdoc/>
    public async Task<List<TrackingSession>> CheckAndShutdownLongRunningSessionsAsync(
        CancellationToken cancellationToken = default)
    {
        var activeSessions = await trackingSessionRepository.GetAllActiveSessionsAsync(cancellationToken);
        var shutdownSessions = new List<TrackingSession>();
        var now = DateTime.UtcNow;

        foreach (var session in activeSessions)
        {
            var user = await userRepository.GetByTelegramUserIdAsync(session.UserId, cancellationToken);
            if (user == null)
            {
                continue;
            }

            var maxHours = GetMaxHoursForState(user, session.State);
            if (!maxHours.HasValue)
            {
                continue; // No limit configured
            }

            var duration = now - session.StartedAt;
            var durationHours = (decimal)duration.TotalHours;

            if (durationHours >= maxHours.Value)
            {
                // Auto-shutdown this session
                session.End(now);
                shutdownSessions.Add(session);
            }
        }

        if (shutdownSessions.Count > 0)
        {
            await unitOfWork.CompleteAsync(cancellationToken);
        }

        return shutdownSessions;
    }

    /// <summary>
    /// Gets the configured maximum hours for a specific tracking state.
    /// </summary>
    private static decimal? GetMaxHoursForState(User user, TrackingState state)
    {
        return state switch
        {
            TrackingState.Working => user.MaxWorkHours,
            TrackingState.Commuting => user.MaxCommuteHours,
            TrackingState.Lunch => user.MaxLunchHours,
            TrackingState.Idle => null,
            _ => null
        };
    }
}
