using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.Enums;
using TimeSheet.Core.Domain.Services;

namespace TimeSheet.Tests.Unit.Services;

/// <summary>
/// Unit tests for TrackingStateMachine.
/// Tests state transition logic and business rules.
/// </summary>
public class TrackingStateMachineTests
{
    private readonly TrackingStateMachine _stateMachine = new();
    private const long TestUserId = 123456789;

    #region Invalid State Tests

    [Fact]
    public void ProcessStateChange_RequestIdleState_ThrowsArgumentException()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _stateMachine.ProcessStateChange(
                TestUserId,
                TrackingState.Idle,
                timestamp,
                currentActiveSession: null,
                lastCommuteDirection: null,
                hasWorkedToday: false));

        Assert.Contains("Cannot explicitly request Idle state", exception.Message);
    }

    #endregion

    #region Start New Session (No Active Session)

    [Theory]
    [InlineData(TrackingState.Working)]
    [InlineData(TrackingState.Lunch)]
    public void ProcessStateChange_NoActiveSession_StartsNewSession(TrackingState requestedState)
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        var result = _stateMachine.ProcessStateChange(
            TestUserId,
            requestedState,
            timestamp,
            currentActiveSession: null,
            lastCommuteDirection: null,
            hasWorkedToday: false);

        // Assert
        Assert.IsType<StateTransitionResult.StartNewSession>(result);
        var startResult = (StateTransitionResult.StartNewSession)result;
        Assert.NotNull(startResult.NewSession);
        Assert.Equal(TestUserId, startResult.NewSession.UserId);
        Assert.Equal(requestedState, startResult.NewSession.State);
        Assert.Equal(timestamp, startResult.NewSession.StartedAt);
        Assert.Null(startResult.SessionToEnd);
    }

    [Fact]
    public void ProcessStateChange_NoActiveSession_FirstCommute_StartsCommuteToWork()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        var result = _stateMachine.ProcessStateChange(
            TestUserId,
            TrackingState.Commuting,
            timestamp,
            currentActiveSession: null,
            lastCommuteDirection: null, // First commute
            hasWorkedToday: false);

        // Assert
        Assert.IsType<StateTransitionResult.StartNewSession>(result);
        var startResult = (StateTransitionResult.StartNewSession)result;
        Assert.NotNull(startResult.NewSession);
        Assert.Equal(TrackingState.Commuting, startResult.NewSession.State);
        Assert.Equal(CommuteDirection.ToWork, startResult.NewSession.CommuteDirection);
    }

    [Fact]
    public void ProcessStateChange_NoActiveSession_SecondCommuteAfterWork_StartsCommuteToHome()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        var result = _stateMachine.ProcessStateChange(
            TestUserId,
            TrackingState.Commuting,
            timestamp,
            currentActiveSession: null,
            lastCommuteDirection: CommuteDirection.ToWork,
            hasWorkedToday: true); // User has worked

        // Assert
        Assert.IsType<StateTransitionResult.StartNewSession>(result);
        var startResult = (StateTransitionResult.StartNewSession)result;
        Assert.NotNull(startResult.NewSession);
        Assert.Equal(TrackingState.Commuting, startResult.NewSession.State);
        Assert.Equal(CommuteDirection.ToHome, startResult.NewSession.CommuteDirection);
    }

    [Fact]
    public void ProcessStateChange_NoActiveSession_SecondCommuteNoWork_AlternatesDirection()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act - user commuted to work but didn't work, now commuting again
        var result = _stateMachine.ProcessStateChange(
            TestUserId,
            TrackingState.Commuting,
            timestamp,
            currentActiveSession: null,
            lastCommuteDirection: CommuteDirection.ToWork,
            hasWorkedToday: false); // No work yet

        // Assert - should alternate to ToHome
        Assert.IsType<StateTransitionResult.StartNewSession>(result);
        var startResult = (StateTransitionResult.StartNewSession)result;
        Assert.Equal(CommuteDirection.ToHome, startResult.NewSession.CommuteDirection);
    }

    #endregion

    #region Toggle Behavior (Same State Requested)

    [Theory]
    [InlineData(TrackingState.Working)]
    [InlineData(TrackingState.Lunch)]
    public void ProcessStateChange_SameStateRequested_EndsSession(TrackingState state)
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddMinutes(-30);
        var endTime = DateTime.UtcNow;
        var activeSession = new TrackingSession(TestUserId, state, startTime);

        // Act
        var result = _stateMachine.ProcessStateChange(
            TestUserId,
            state, // Same state
            endTime,
            currentActiveSession: activeSession,
            lastCommuteDirection: null,
            hasWorkedToday: false);

        // Assert
        Assert.IsType<StateTransitionResult.EndSession>(result);
        var endResult = (StateTransitionResult.EndSession)result;
        Assert.Equal(activeSession, endResult.SessionToEnd);
    }

    [Fact]
    public void ProcessStateChange_CommuteToggle_EndsSession()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddMinutes(-20);
        var endTime = DateTime.UtcNow;
        var activeSession = new TrackingSession(
            TestUserId,
            TrackingState.Commuting,
            startTime,
            CommuteDirection.ToWork);

        // Act - request commute again (toggle)
        var result = _stateMachine.ProcessStateChange(
            TestUserId,
            TrackingState.Commuting,
            endTime,
            currentActiveSession: activeSession,
            lastCommuteDirection: CommuteDirection.ToWork,
            hasWorkedToday: false);

        // Assert
        Assert.IsType<StateTransitionResult.EndSession>(result);
        var endResult = (StateTransitionResult.EndSession)result;
        Assert.Equal(activeSession, endResult.SessionToEnd);
    }

    #endregion

    #region Exclusive State Behavior (Different State Requested)

    [Fact]
    public void ProcessStateChange_DifferentStateRequested_EndsCurrentAndStartsNew()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddMinutes(-30);
        var transitionTime = DateTime.UtcNow;
        var activeSession = new TrackingSession(
            TestUserId,
            TrackingState.Commuting,
            startTime,
            CommuteDirection.ToWork);

        // Act - switch to working
        var result = _stateMachine.ProcessStateChange(
            TestUserId,
            TrackingState.Working,
            transitionTime,
            currentActiveSession: activeSession,
            lastCommuteDirection: CommuteDirection.ToWork,
            hasWorkedToday: false);

        // Assert
        Assert.IsType<StateTransitionResult.StartNewSession>(result);
        var startResult = (StateTransitionResult.StartNewSession)result;

        Assert.NotNull(startResult.NewSession);
        Assert.Equal(TrackingState.Working, startResult.NewSession.State);
        Assert.Equal(transitionTime, startResult.NewSession.StartedAt);

        Assert.NotNull(startResult.SessionToEnd);
        Assert.Equal(activeSession, startResult.SessionToEnd);
    }

    [Fact]
    public void ProcessStateChange_WorkToLunch_EndsWorkAndStartsLunch()
    {
        // Arrange
        var workStartTime = DateTime.UtcNow.AddHours(-4);
        var lunchStartTime = DateTime.UtcNow;
        var activeSession = new TrackingSession(TestUserId, TrackingState.Working, workStartTime);

        // Act
        var result = _stateMachine.ProcessStateChange(
            TestUserId,
            TrackingState.Lunch,
            lunchStartTime,
            currentActiveSession: activeSession,
            lastCommuteDirection: null,
            hasWorkedToday: true);

        // Assert
        Assert.IsType<StateTransitionResult.StartNewSession>(result);
        var startResult = (StateTransitionResult.StartNewSession)result;

        Assert.Equal(TrackingState.Lunch, startResult.NewSession.State);
        Assert.Equal(activeSession, startResult.SessionToEnd);
    }

    [Fact]
    public void ProcessStateChange_LunchToWork_EndsLunchAndStartsWork()
    {
        // Arrange
        var lunchStartTime = DateTime.UtcNow.AddMinutes(-30);
        var workStartTime = DateTime.UtcNow;
        var activeSession = new TrackingSession(TestUserId, TrackingState.Lunch, lunchStartTime);

        // Act
        var result = _stateMachine.ProcessStateChange(
            TestUserId,
            TrackingState.Working,
            workStartTime,
            currentActiveSession: activeSession,
            lastCommuteDirection: null,
            hasWorkedToday: true);

        // Assert
        Assert.IsType<StateTransitionResult.StartNewSession>(result);
        var startResult = (StateTransitionResult.StartNewSession)result;

        Assert.Equal(TrackingState.Working, startResult.NewSession.State);
        Assert.Equal(activeSession, startResult.SessionToEnd);
    }

    [Fact]
    public void ProcessStateChange_WorkToCommute_StartsCommuteToHome()
    {
        // Arrange
        var workStartTime = DateTime.UtcNow.AddHours(-8);
        var commuteStartTime = DateTime.UtcNow;
        var activeSession = new TrackingSession(TestUserId, TrackingState.Working, workStartTime);

        // Act
        var result = _stateMachine.ProcessStateChange(
            TestUserId,
            TrackingState.Commuting,
            commuteStartTime,
            currentActiveSession: activeSession,
            lastCommuteDirection: CommuteDirection.ToWork, // They commuted to work earlier
            hasWorkedToday: true);

        // Assert
        Assert.IsType<StateTransitionResult.StartNewSession>(result);
        var startResult = (StateTransitionResult.StartNewSession)result;

        Assert.Equal(TrackingState.Commuting, startResult.NewSession.State);
        Assert.Equal(CommuteDirection.ToHome, startResult.NewSession.CommuteDirection);
        Assert.Equal(activeSession, startResult.SessionToEnd);
    }

    #endregion

    #region Commute Direction Logic

    [Fact]
    public void ProcessStateChange_FirstCommuteOfDay_AlwaysToWork()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        var result = _stateMachine.ProcessStateChange(
            TestUserId,
            TrackingState.Commuting,
            timestamp,
            currentActiveSession: null,
            lastCommuteDirection: null, // No previous commute
            hasWorkedToday: false);

        // Assert
        var startResult = (StateTransitionResult.StartNewSession)result;
        Assert.Equal(CommuteDirection.ToWork, startResult.NewSession.CommuteDirection);
    }

    [Fact]
    public void ProcessStateChange_CommuteAfterWork_AlwaysToHome()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        var result = _stateMachine.ProcessStateChange(
            TestUserId,
            TrackingState.Commuting,
            timestamp,
            currentActiveSession: null,
            lastCommuteDirection: CommuteDirection.ToWork,
            hasWorkedToday: true); // User has worked

        // Assert
        var startResult = (StateTransitionResult.StartNewSession)result;
        Assert.Equal(CommuteDirection.ToHome, startResult.NewSession.CommuteDirection);
    }

    [Fact]
    public void ProcessStateChange_SecondCommuteWithoutWork_AlternatesFromToWorkToToHome()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act - last commute was ToWork, but user hasn't worked yet
        var result = _stateMachine.ProcessStateChange(
            TestUserId,
            TrackingState.Commuting,
            timestamp,
            currentActiveSession: null,
            lastCommuteDirection: CommuteDirection.ToWork,
            hasWorkedToday: false);

        // Assert
        var startResult = (StateTransitionResult.StartNewSession)result;
        Assert.Equal(CommuteDirection.ToHome, startResult.NewSession.CommuteDirection);
    }

    [Fact]
    public void ProcessStateChange_SecondCommuteWithoutWork_AlternatesFromToHomeToToWork()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act - last commute was ToHome, but user hasn't worked yet
        var result = _stateMachine.ProcessStateChange(
            TestUserId,
            TrackingState.Commuting,
            timestamp,
            currentActiveSession: null,
            lastCommuteDirection: CommuteDirection.ToHome,
            hasWorkedToday: false);

        // Assert
        var startResult = (StateTransitionResult.StartNewSession)result;
        Assert.Equal(CommuteDirection.ToWork, startResult.NewSession.CommuteDirection);
    }

    #endregion

    #region Complex Scenarios

    [Fact]
    public void ProcessStateChange_TypicalWorkdayFlow_HandlesAllTransitions()
    {
        // Scenario: Commute -> Work -> Lunch -> Work -> Commute (home)

        var baseTime = DateTime.UtcNow.Date.AddHours(8); // 8 AM today
        TrackingSession? currentSession = null;
        var lastCommute = (CommuteDirection?)null;
        var hasWorked = false;

        // 1. Start commute to work (8:00 AM)
        var result1 = _stateMachine.ProcessStateChange(
            TestUserId,
            TrackingState.Commuting,
            baseTime,
            currentSession,
            lastCommute,
            hasWorked);

        var start1 = (StateTransitionResult.StartNewSession)result1;
        Assert.Equal(CommuteDirection.ToWork, start1.NewSession.CommuteDirection);
        currentSession = start1.NewSession;
        lastCommute = CommuteDirection.ToWork;

        // 2. Arrive at work, start working (8:30 AM)
        var result2 = _stateMachine.ProcessStateChange(
            TestUserId,
            TrackingState.Working,
            baseTime.AddMinutes(30),
            currentSession,
            lastCommute,
            hasWorked);

        var start2 = (StateTransitionResult.StartNewSession)result2;
        Assert.Equal(TrackingState.Working, start2.NewSession.State);
        Assert.Equal(currentSession, start2.SessionToEnd);
        currentSession = start2.NewSession;
        hasWorked = true;

        // 3. Go to lunch (12:00 PM)
        var result3 = _stateMachine.ProcessStateChange(
            TestUserId,
            TrackingState.Lunch,
            baseTime.AddHours(4),
            currentSession,
            lastCommute,
            hasWorked);

        var start3 = (StateTransitionResult.StartNewSession)result3;
        Assert.Equal(TrackingState.Lunch, start3.NewSession.State);
        currentSession = start3.NewSession;

        // 4. Return to work (12:30 PM)
        var result4 = _stateMachine.ProcessStateChange(
            TestUserId,
            TrackingState.Working,
            baseTime.AddHours(4).AddMinutes(30),
            currentSession,
            lastCommute,
            hasWorked);

        var start4 = (StateTransitionResult.StartNewSession)result4;
        Assert.Equal(TrackingState.Working, start4.NewSession.State);
        currentSession = start4.NewSession;

        // 5. Start commute home (5:00 PM)
        var result5 = _stateMachine.ProcessStateChange(
            TestUserId,
            TrackingState.Commuting,
            baseTime.AddHours(9),
            currentSession,
            lastCommute,
            hasWorked);

        var start5 = (StateTransitionResult.StartNewSession)result5;
        Assert.Equal(CommuteDirection.ToHome, start5.NewSession.CommuteDirection);
    }

    #endregion
}
