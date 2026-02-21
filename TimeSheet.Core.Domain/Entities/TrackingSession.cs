using TimeSheet.Core.Domain.Enums;
using TimeSheet.Core.Domain.SharedKernel;

namespace TimeSheet.Core.Domain.Entities;

/// <summary>
/// Represents a time tracking session for a user.
/// A session tracks a continuous period of time spent in a particular state (commuting, working, or lunch).
/// </summary>
public class TrackingSession : BaseEntity
{
    /// <summary>
    /// Gets the Telegram user ID who owns this tracking session.
    /// </summary>
    public long UserId { get; }

    /// <summary>
    /// Gets the tracking state of this session.
    /// </summary>
    public TrackingState State { get; }

    /// <summary>
    /// Gets the UTC timestamp when this session started.
    /// </summary>
    public DateTime StartedAt { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when this session ended.
    /// Null indicates the session is still active.
    /// </summary>
    public DateTime? EndedAt { get; private set; }

    /// <summary>
    /// Gets the direction of commute (only applicable when State is Commuting).
    /// Null for non-commuting states.
    /// </summary>
    public CommuteDirection? CommuteDirection { get; }

    /// <summary>
    /// Gets whether this session is currently active (not yet ended).
    /// </summary>
    public bool IsActive => EndedAt == null;

    /// <summary>
    /// Gets an optional note attached to this session.
    /// </summary>
    public string? Note { get; private set; }

    /// <summary>
    /// Updates the note on this session. Null or whitespace clears the note.
    /// </summary>
    public void UpdateNote(string? note)
    {
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TrackingSession"/> class.
    /// Used when creating a new tracking session.
    /// </summary>
    /// <param name="userId">The Telegram user ID.</param>
    /// <param name="state">The tracking state.</param>
    /// <param name="startedAt">The UTC timestamp when the session started.</param>
    /// <param name="commuteDirection">The commute direction (required when state is Commuting, null otherwise).</param>
    /// <exception cref="ArgumentException">
    /// Thrown when commuteDirection is null while state is Commuting,
    /// or when commuteDirection is not null while state is not Commuting.
    /// </exception>
    public TrackingSession(
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
    /// Initializes a new instance of the <see cref="TrackingSession"/> class with a specified ID.
    /// Used for entity rehydration from persistence.
    /// </summary>
    /// <param name="id">The unique identifier for this entity.</param>
    /// <param name="userId">The Telegram user ID.</param>
    /// <param name="state">The tracking state.</param>
    /// <param name="startedAt">The UTC timestamp when the session started.</param>
    /// <param name="endedAt">The UTC timestamp when the session ended (null if active).</param>
    /// <param name="commuteDirection">The commute direction (required when state is Commuting, null otherwise).</param>
    /// <exception cref="ArgumentException">
    /// Thrown when commuteDirection is null while state is Commuting,
    /// or when commuteDirection is not null while state is not Commuting.
    /// </exception>
    public TrackingSession(
        Guid id,
        long userId,
        TrackingState state,
        DateTime startedAt,
        DateTime? endedAt,
        CommuteDirection? commuteDirection = null)
        : base(id)
    {
        ValidateCommuteDirection(state, commuteDirection);

        UserId = userId;
        State = state;
        StartedAt = startedAt;
        EndedAt = endedAt;
        CommuteDirection = commuteDirection;
    }

    /// <summary>
    /// Ends this tracking session at the specified UTC timestamp.
    /// </summary>
    /// <param name="endedAt">The UTC timestamp when the session ended.</param>
    /// <exception cref="InvalidOperationException">Thrown when the session is already ended.</exception>
    /// <exception cref="ArgumentException">Thrown when endedAt is before StartedAt.</exception>
    public void End(DateTime endedAt)
    {
        if (!IsActive)
            throw new InvalidOperationException("Cannot end a session that is already ended.");

        if (endedAt < StartedAt)
            throw new ArgumentException("End time cannot be before start time.", nameof(endedAt));

        EndedAt = endedAt;
    }

    /// <summary>
    /// Adjusts the end time of a completed session by a specified number of minutes.
    /// Positive values extend the session, negative values shorten it.
    /// </summary>
    /// <param name="adjustmentMinutes">The number of minutes to adjust (positive to extend, negative to shorten).</param>
    /// <exception cref="InvalidOperationException">Thrown when the session is still active (not ended).</exception>
    /// <exception cref="ArgumentException">Thrown when the adjustment would result in end time before start time.</exception>
    public void AdjustEndTime(int adjustmentMinutes)
    {
        if (IsActive)
            throw new InvalidOperationException("Cannot adjust an active session. End the session first.");

        var newEndTime = EndedAt!.Value.AddMinutes(adjustmentMinutes);

        if (newEndTime <= StartedAt)
            throw new ArgumentException(
                $"Adjustment of {adjustmentMinutes} minutes would result in end time before or equal to start time.",
                nameof(adjustmentMinutes));

        EndedAt = newEndTime;
    }

    /// <summary>
    /// Adjusts the start time of a session by a specified number of minutes.
    /// </summary>
    public void AdjustStartTime(int adjustmentMinutes)
    {
        var newStartTime = StartedAt.AddMinutes(adjustmentMinutes);
        if (EndedAt.HasValue && newStartTime >= EndedAt.Value)
            throw new ArgumentException(
                $"Adjustment of {adjustmentMinutes} minutes would result in start time at or after end time.",
                nameof(adjustmentMinutes));
        if (newStartTime > DateTime.UtcNow.AddMinutes(5))
            throw new ArgumentException("Start time cannot be in the future.", nameof(adjustmentMinutes));
        StartedAt = newStartTime;
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
