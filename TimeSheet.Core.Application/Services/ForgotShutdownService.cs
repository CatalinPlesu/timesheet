using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using TimeSheet.Core.Application.Interfaces.Services;
using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.Enums;
using TimeSheet.Core.Domain.Repositories;

namespace TimeSheet.Core.Application.Services;

/// <summary>
/// Service for detecting and notifying users about long-running sessions that may have been forgotten.
/// </summary>
public sealed class ForgotShutdownService(
    ITrackingSessionRepository trackingSessionRepository,
    IUserRepository userRepository,
    INotificationService notificationService,
    ILogger<ForgotShutdownService> logger) : IForgotShutdownService
{
    // Track which sessions have already been notified (session ID -> notification time)
    // This prevents spamming users with multiple reminders for the same session
    private static readonly ConcurrentDictionary<Guid, DateTime> NotifiedSessions = new();

    /// <inheritdoc/>
    public async Task<List<TrackingSession>> CheckAndNotifyLongRunningSessionsAsync(CancellationToken cancellationToken = default)
    {
        var notifiedSessions = new List<TrackingSession>();

        // Get all active sessions
        var activeSessions = await trackingSessionRepository.GetAllActiveSessionsAsync(cancellationToken);

        logger.LogDebug("Checking {Count} active session(s) for forgot-shutdown detection", activeSessions.Count);

        foreach (var session in activeSessions)
        {
            // Skip if we've already notified for this session
            if (NotifiedSessions.ContainsKey(session.Id))
            {
                continue;
            }

            // Check if this session should trigger a reminder
            if (await ShouldNotifyForSessionAsync(session, cancellationToken))
            {
                // Get the user to retrieve their settings
                var user = await userRepository.GetByTelegramUserIdAsync(session.UserId, cancellationToken);
                if (user == null)
                {
                    logger.LogWarning("User {UserId} not found for session {SessionId}", session.UserId, session.Id);
                    continue;
                }

                // Calculate average duration for this state
                var averageDuration = await trackingSessionRepository.GetAverageDurationAsync(
                    session.UserId,
                    session.State,
                    cancellationToken);

                if (!averageDuration.HasValue)
                {
                    // No historical data - skip notification
                    continue;
                }

                // Calculate current duration
                var currentDuration = (decimal)(DateTime.UtcNow - session.StartedAt).TotalHours;

                // Send notification
                await notificationService.SendForgotShutdownReminderAsync(
                    session.UserId,
                    session.State,
                    currentDuration,
                    averageDuration.Value,
                    cancellationToken);

                // Mark this session as notified
                NotifiedSessions.TryAdd(session.Id, DateTime.UtcNow);
                notifiedSessions.Add(session);

                logger.LogInformation(
                    "Sent forgot-shutdown reminder for session {SessionId} (user: {UserId}, state: {State})",
                    session.Id,
                    session.UserId,
                    session.State);
            }
        }

        // Clean up old entries (sessions that were notified but are now ended)
        CleanupNotifiedSessions();

        return notifiedSessions;
    }

    /// <inheritdoc/>
    public async Task<bool> ShouldNotifyForSessionAsync(TrackingSession session, CancellationToken cancellationToken = default)
    {
        // Only check active sessions
        if (!session.IsActive)
        {
            return false;
        }

        // Get the user to check their threshold setting
        var user = await userRepository.GetByTelegramUserIdAsync(session.UserId, cancellationToken);
        if (user == null || !user.ForgotShutdownThresholdPercent.HasValue)
        {
            // User not found or threshold not configured - no notification
            return false;
        }

        // Calculate average duration for this tracking state
        var averageDuration = await trackingSessionRepository.GetAverageDurationAsync(
            session.UserId,
            session.State,
            cancellationToken);

        if (!averageDuration.HasValue)
        {
            // No historical data to compare against - no notification
            return false;
        }

        // Calculate current duration
        var currentDuration = (decimal)(DateTime.UtcNow - session.StartedAt).TotalHours;

        // Calculate threshold
        var thresholdDuration = averageDuration.Value * (user.ForgotShutdownThresholdPercent.Value / 100m);

        // Check if current duration exceeds threshold
        return currentDuration >= thresholdDuration;
    }

    /// <summary>
    /// Removes old entries from the notified sessions cache.
    /// Entries older than 24 hours are removed.
    /// </summary>
    private static void CleanupNotifiedSessions()
    {
        var cutoff = DateTime.UtcNow.AddHours(-24);
        var keysToRemove = NotifiedSessions
            .Where(kvp => kvp.Value < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            NotifiedSessions.TryRemove(key, out _);
        }
    }
}
