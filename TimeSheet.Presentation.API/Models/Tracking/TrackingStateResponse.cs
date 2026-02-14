using TimeSheet.Core.Domain.Enums;

namespace TimeSheet.Presentation.API.Models.Tracking;

/// <summary>
/// Response model for tracking state toggle operation.
/// </summary>
public sealed class TrackingStateResponse
{
    /// <summary>
    /// Gets or sets the new tracking state after the toggle.
    /// </summary>
    public required TrackingState NewState { get; set; }

    /// <summary>
    /// Gets or sets the previous state that was ended (if any).
    /// </summary>
    public TrackingState? PreviousState { get; set; }

    /// <summary>
    /// Gets or sets a message describing what happened.
    /// </summary>
    /// <example>Started working at 9:00 AM</example>
    public required string Message { get; set; }

    /// <summary>
    /// Gets or sets the time when the new state started (UTC).
    /// Null if the new state is Idle.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the duration of the previous session in hours (if any).
    /// </summary>
    public decimal? PreviousSessionDurationHours { get; set; }
}
