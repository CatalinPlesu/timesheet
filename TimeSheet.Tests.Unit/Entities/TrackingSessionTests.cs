using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.Enums;

namespace TimeSheet.Tests.Unit.Entities;

/// <summary>
/// Unit tests for TrackingSession entity.
/// Tests entity behavior, validation, and state management.
/// </summary>
public class TrackingSessionTests
{
    private const long TestUserId = 123456789;

    #region Constructor Tests

    [Theory]
    [InlineData(TrackingState.Working)]
    [InlineData(TrackingState.Lunch)]
    public void Constructor_NonCommutingState_CreatesSessionWithoutDirection(TrackingState state)
    {
        // Arrange
        var startTime = DateTime.UtcNow;

        // Act
        var session = new TrackingSession(TestUserId, state, startTime);

        // Assert
        Assert.Equal(TestUserId, session.UserId);
        Assert.Equal(state, session.State);
        Assert.Equal(startTime, session.StartedAt);
        Assert.Null(session.EndedAt);
        Assert.Null(session.CommuteDirection);
        Assert.True(session.IsActive);
    }

    [Theory]
    [InlineData(CommuteDirection.ToWork)]
    [InlineData(CommuteDirection.ToHome)]
    public void Constructor_CommutingState_CreatesSessionWithDirection(CommuteDirection direction)
    {
        // Arrange
        var startTime = DateTime.UtcNow;

        // Act
        var session = new TrackingSession(
            TestUserId,
            TrackingState.Commuting,
            startTime,
            direction);

        // Assert
        Assert.Equal(TestUserId, session.UserId);
        Assert.Equal(TrackingState.Commuting, session.State);
        Assert.Equal(startTime, session.StartedAt);
        Assert.Null(session.EndedAt);
        Assert.Equal(direction, session.CommuteDirection);
        Assert.True(session.IsActive);
    }

    [Fact]
    public void Constructor_CommutingStateWithoutDirection_ThrowsArgumentException()
    {
        // Arrange
        var startTime = DateTime.UtcNow;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new TrackingSession(TestUserId, TrackingState.Commuting, startTime, commuteDirection: null));

        Assert.Contains("must be specified when state is Commuting", exception.Message);
    }

    [Theory]
    [InlineData(TrackingState.Working)]
    [InlineData(TrackingState.Lunch)]
    public void Constructor_NonCommutingStateWithDirection_ThrowsArgumentException(TrackingState state)
    {
        // Arrange
        var startTime = DateTime.UtcNow;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new TrackingSession(TestUserId, state, startTime, CommuteDirection.ToWork));

        Assert.Contains("should only be specified when state is Commuting", exception.Message);
    }

    #endregion

    #region Rehydration Constructor Tests

    [Fact]
    public void RehydrationConstructor_ValidParameters_CreatesSession()
    {
        // Arrange
        var id = Guid.NewGuid();
        var startTime = DateTime.UtcNow.AddMinutes(-30);
        var endTime = DateTime.UtcNow;

        // Act
        var session = new TrackingSession(
            id,
            TestUserId,
            TrackingState.Working,
            startTime,
            endTime);

        // Assert
        Assert.Equal(id, session.Id);
        Assert.Equal(TestUserId, session.UserId);
        Assert.Equal(TrackingState.Working, session.State);
        Assert.Equal(startTime, session.StartedAt);
        Assert.Equal(endTime, session.EndedAt);
        Assert.False(session.IsActive);
    }

    [Fact]
    public void RehydrationConstructor_CommutingState_RequiresDirection()
    {
        // Arrange
        var id = Guid.NewGuid();
        var startTime = DateTime.UtcNow.AddMinutes(-30);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new TrackingSession(
                id,
                TestUserId,
                TrackingState.Commuting,
                startTime,
                endedAt: null,
                commuteDirection: null));

        Assert.Contains("must be specified when state is Commuting", exception.Message);
    }

    #endregion

    #region End Method Tests

    [Fact]
    public void End_ActiveSession_EndsSession()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddMinutes(-30);
        var endTime = DateTime.UtcNow;
        var session = new TrackingSession(TestUserId, TrackingState.Working, startTime);

        // Act
        session.End(endTime);

        // Assert
        Assert.Equal(endTime, session.EndedAt);
        Assert.False(session.IsActive);
    }

    [Fact]
    public void End_AlreadyEndedSession_ThrowsInvalidOperationException()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddMinutes(-60);
        var firstEndTime = DateTime.UtcNow.AddMinutes(-30);
        var session = new TrackingSession(TestUserId, TrackingState.Working, startTime);
        session.End(firstEndTime);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            session.End(DateTime.UtcNow));

        Assert.Contains("already ended", exception.Message);
    }

    [Fact]
    public void End_EndTimeBeforeStartTime_ThrowsArgumentException()
    {
        // Arrange
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddMinutes(-10); // Before start
        var session = new TrackingSession(TestUserId, TrackingState.Working, startTime);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            session.End(endTime));

        Assert.Contains("End time cannot be before start time", exception.Message);
    }

    [Fact]
    public void End_EndTimeSameAsStartTime_Succeeds()
    {
        // Arrange
        var startTime = DateTime.UtcNow;
        var endTime = startTime; // Same time
        var session = new TrackingSession(TestUserId, TrackingState.Working, startTime);

        // Act
        session.End(endTime);

        // Assert
        Assert.Equal(endTime, session.EndedAt);
        Assert.False(session.IsActive);
    }

    #endregion

    #region IsActive Property Tests

    [Fact]
    public void IsActive_NewSession_ReturnsTrue()
    {
        // Arrange
        var session = new TrackingSession(TestUserId, TrackingState.Working, DateTime.UtcNow);

        // Act & Assert
        Assert.True(session.IsActive);
    }

    [Fact]
    public void IsActive_EndedSession_ReturnsFalse()
    {
        // Arrange
        var session = new TrackingSession(TestUserId, TrackingState.Working, DateTime.UtcNow.AddMinutes(-30));
        session.End(DateTime.UtcNow);

        // Act & Assert
        Assert.False(session.IsActive);
    }

    [Fact]
    public void IsActive_RehydratedActiveSession_ReturnsTrue()
    {
        // Arrange
        var session = new TrackingSession(
            Guid.NewGuid(),
            TestUserId,
            TrackingState.Working,
            DateTime.UtcNow.AddMinutes(-30),
            endedAt: null);

        // Act & Assert
        Assert.True(session.IsActive);
    }

    [Fact]
    public void IsActive_RehydratedEndedSession_ReturnsFalse()
    {
        // Arrange
        var session = new TrackingSession(
            Guid.NewGuid(),
            TestUserId,
            TrackingState.Working,
            DateTime.UtcNow.AddMinutes(-60),
            DateTime.UtcNow.AddMinutes(-30));

        // Act & Assert
        Assert.False(session.IsActive);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Constructor_FutureStartTime_CreatesSession()
    {
        // Arrange
        var futureStartTime = DateTime.UtcNow.AddHours(1);

        // Act
        var session = new TrackingSession(TestUserId, TrackingState.Working, futureStartTime);

        // Assert
        Assert.Equal(futureStartTime, session.StartedAt);
        Assert.True(session.IsActive);
    }

    [Fact]
    public void End_FutureEndTime_Succeeds()
    {
        // Arrange
        var startTime = DateTime.UtcNow;
        var futureEndTime = startTime.AddHours(2);
        var session = new TrackingSession(TestUserId, TrackingState.Working, startTime);

        // Act
        session.End(futureEndTime);

        // Assert
        Assert.Equal(futureEndTime, session.EndedAt);
        Assert.False(session.IsActive);
    }

    [Theory]
    [InlineData(TrackingState.Working)]
    [InlineData(TrackingState.Lunch)]
    [InlineData(TrackingState.Commuting)]
    public void Constructor_AllValidStates_CreatesSessions(TrackingState state)
    {
        // Arrange
        var startTime = DateTime.UtcNow;
        var direction = state == TrackingState.Commuting ? CommuteDirection.ToWork : (CommuteDirection?)null;

        // Act
        var session = new TrackingSession(TestUserId, state, startTime, direction);

        // Assert
        Assert.Equal(state, session.State);
        Assert.NotEqual(Guid.Empty, session.Id);
    }

    #endregion
}
