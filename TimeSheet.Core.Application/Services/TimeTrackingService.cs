using TimeSheet.Core.Application.Interfaces;
using TimeSheet.Core.Application.Models;
using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.Enums;
using TimeSheet.Core.Domain.Services;

namespace TimeSheet.Core.Application.Services;

/// <summary>
/// Application service that orchestrates time tracking operations.
/// Coordinates between domain logic, repositories, and persistence.
/// </summary>
/// <param name="trackingSessionRepository">Repository for tracking sessions.</param>
/// <param name="stateMachine">Domain service for state transition logic.</param>
/// <param name="unitOfWork">Unit of work for transaction management.</param>
public class TimeTrackingService(
    ITrackingSessionRepository trackingSessionRepository,
    ITrackingStateMachine stateMachine,
    IUnitOfWork unitOfWork) : ITimeTrackingService
{
    /// <inheritdoc/>
    public async Task<TrackingResult> StartStateAsync(
        long userId,
        TrackingState targetState,
        DateTime timestamp,
        CancellationToken cancellationToken = default)
    {
        // Get current state
        var currentActiveSession = await trackingSessionRepository.GetActiveSessionAsync(userId, cancellationToken);

        // Get today's date in UTC for context queries
        var today = timestamp.Date;

        // Get last commute direction
        var lastCommuteSession = await trackingSessionRepository.GetLastCommuteSessionAsync(userId, today, cancellationToken);
        var lastCommuteDirection = lastCommuteSession?.CommuteDirection;

        // Check if user has worked today
        var hasWorkedToday = await trackingSessionRepository.HasWorkedTodayAsync(userId, today, cancellationToken);

        // Process the state transition using domain logic
        var transitionResult = stateMachine.ProcessStateChange(
            userId,
            targetState,
            timestamp,
            currentActiveSession,
            lastCommuteDirection,
            hasWorkedToday);

        // Apply the result to persistence
        TrackingResult result = transitionResult switch
        {
            StateTransitionResult.EndSession endSession =>
                await HandleEndSessionAsync(endSession, timestamp, cancellationToken),

            StateTransitionResult.StartNewSession startNewSession =>
                await HandleStartNewSessionAsync(startNewSession, timestamp, cancellationToken),

            StateTransitionResult.NoChange =>
                TrackingResult.NoChange.Instance,

            _ => throw new InvalidOperationException($"Unknown transition result type: {transitionResult.GetType().Name}")
        };

        // Commit changes
        await unitOfWork.CompleteAsync(cancellationToken);

        return result;
    }

    /// <summary>
    /// Handles ending a session (toggle behavior).
    /// </summary>
    private async Task<TrackingResult> HandleEndSessionAsync(
        StateTransitionResult.EndSession endSession,
        DateTime timestamp,
        CancellationToken cancellationToken)
    {
        var session = endSession.SessionToEnd;
        session.End(timestamp);
        trackingSessionRepository.Update(session);

        return new TrackingResult.SessionEnded(session);
    }

    /// <summary>
    /// Handles starting a new session, optionally ending the current one (exclusive state behavior).
    /// </summary>
    private async Task<TrackingResult> HandleStartNewSessionAsync(
        StateTransitionResult.StartNewSession startNewSession,
        DateTime timestamp,
        CancellationToken cancellationToken)
    {
        // End the current session if there is one (exclusive state behavior)
        if (startNewSession.SessionToEnd != null)
        {
            var sessionToEnd = startNewSession.SessionToEnd;
            sessionToEnd.End(timestamp);
            trackingSessionRepository.Update(sessionToEnd);
        }

        // Start the new session
        var newSession = startNewSession.NewSession;
        await trackingSessionRepository.AddAsync(newSession, cancellationToken);

        return new TrackingResult.SessionStarted(newSession, startNewSession.SessionToEnd);
    }

    /// <inheritdoc/>
    public async Task<List<TrackingSession>> GetTodaysSessionsAsync(
        long userId,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        return await trackingSessionRepository.GetSessionsByDateAsync(userId, date, cancellationToken);
    }
}
