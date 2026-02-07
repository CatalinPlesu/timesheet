using TimeSheet.Core.Domain.Enums;
using TimeSheet.Core.Domain.SharedKernel;

namespace TimeSheet.Core.Domain.Entities;

/// <summary>
/// Represents a completed time tracking entry.
/// TimeEntry records historical work sessions, while TrackingSession tracks current state.
/// </summary>
public class TimeEntry : CreatedEntity
{
    /// <summary>
    /// Gets the Telegram user ID who owns this time entry.
    /// </summary>
    public long UserId { get; }

    /// <summary>
    /// Gets the tracking state for this entry.
    /// </summary>
    public TrackingState State { get; }

    /// <summary>
    /// Gets the UTC timestamp when this entry started.
    /// </summary>
    public DateTime StartedAt { get; }

    /// <summary>
    /// Gets the UTC timestamp when this entry ended.
    /// Null indicates the entry is still active (currently being tracked).
    /// </summary>
    public DateTime? EndedAt { get; private set; }

    /// <summary>
    /// Gets the direction of commute (only applicable when State is Commuting).
    /// Null for non-commuting states.
    /// </summary>
    public CommuteDirection? CommuteDirection { get; }

    /// <summary>
    /// Gets whether this entry is currently active (not yet ended).
    /// </summary>
    public bool IsActive => EndedAt == null;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimeEntry"/> class.
    /// Used when creating a new time entry.
    /// </summary>
    /// <param name="userId">The Telegram user ID.</param>
    /// <param name="state">The tracking state.</param>
    /// <param name="startedAt">The UTC timestamp when the entry started.</param>
    /// <param name="commuteDirection">The commute direction (required when state is Commuting, null otherwise).</param>
    /// <exception cref="ArgumentException">
    /// Thrown when commuteDirection is null while state is Commuting,
    /// or when commuteDirection is not null while state is not Commuting.
    /// </exception>
    public TimeEntry(
        long userId,
        TrackingState state,
        DateTime startedAt,
        CommuteDirection? commuteDirection = null)
    {
        ValidateCommuteDirection(state, commuteDirection);

        UserId = userId;
        State = state;
        StartedAt = startedAt;
        CommuteDirection = commuteDirection;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TimeEntry"/> class with a specified ID and creation timestamp.
    /// Used for entity rehydration from persistence.
    /// </summary>
    /// <param name="id">The unique identifier for this entity.</param>
    /// <param name="createdAt">The UTC timestamp when this entity was created.</param>
    /// <param name="userId">The Telegram user ID.</param>
    /// <param name="state">The tracking state.</param>
    /// <param name="startedAt">The UTC timestamp when the entry started.</param>
    /// <param name="endedAt">The UTC timestamp when the entry ended (null if active).</param>
    /// <param name="commuteDirection">The commute direction (required when state is Commuting, null otherwise).</param>
    /// <exception cref="ArgumentException">
    /// Thrown when commuteDirection is null while state is Commuting,
    /// or when commuteDirection is not null while state is not Commuting.
    /// </exception>
    public TimeEntry(
        Guid id,
        DateTimeOffset createdAt,
        long userId,
        TrackingState state,
        DateTime startedAt,
        DateTime? endedAt,
        CommuteDirection? commuteDirection = null)
        : base(id, createdAt)
    {
        ValidateCommuteDirection(state, commuteDirection);

        UserId = userId;
        State = state;
        StartedAt = startedAt;
        EndedAt = endedAt;
        CommuteDirection = commuteDirection;
    }

    /// <summary>
    /// Ends this time entry at the specified UTC timestamp.
    /// </summary>
    /// <param name="endTime">The UTC timestamp when the entry ended.</param>
    /// <exception cref="InvalidOperationException">Thrown when the entry is already ended.</exception>
    /// <exception cref="ArgumentException">Thrown when endTime is before StartedAt.</exception>
    public void End(DateTime endTime)
    {
        if (!IsActive)
            throw new InvalidOperationException("Cannot end an entry that is already ended.");

        if (endTime < StartedAt)
            throw new ArgumentException("End time cannot be before start time.", nameof(endTime));

        EndedAt = endTime;
    }

    /// <summary>
    /// Validates that commute direction is provided if and only if the state is Commuting.
    /// </summary>
    private static void ValidateCommuteDirection(TrackingState state, CommuteDirection? commuteDirection)
    {
        if (state == TrackingState.Commuting && commuteDirection == null)
            throw new ArgumentException("Commute direction must be specified when state is Commuting.", nameof(commuteDirection));

        if (state != TrackingState.Commuting && commuteDirection != null)
            throw new ArgumentException("Commute direction should only be specified when state is Commuting.", nameof(commuteDirection));
    }
}
