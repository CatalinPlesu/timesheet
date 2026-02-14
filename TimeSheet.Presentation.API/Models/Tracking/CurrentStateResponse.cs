using TimeSheet.Core.Domain.Enums;

namespace TimeSheet.Presentation.API.Models.Tracking;

/// <summary>
/// Response model for current tracking state.
/// </summary>
public sealed class CurrentStateResponse
{
    /// <summary>
    /// Gets or sets the current tracking state.
    /// </summary>
    public required TrackingState State { get; set; }

    /// <summary>
    /// Gets or sets the time when the current state started (UTC).
    /// Null if the user is idle.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the duration of the current state in hours.
    /// Null if the user is idle.
    /// </summary>
    public decimal? DurationHours { get; set; }

    /// <summary>
    /// Gets or sets the commute direction (only applicable when State is Commuting).
    /// </summary>
    public CommuteDirection? CommuteDirection { get; set; }
}
