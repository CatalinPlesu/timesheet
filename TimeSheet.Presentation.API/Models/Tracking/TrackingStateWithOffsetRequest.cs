using TimeSheet.Core.Domain.Enums;

namespace TimeSheet.Presentation.API.Models.Tracking;

/// <summary>
/// Request model for toggling tracking state with time offset.
/// </summary>
public sealed class TrackingStateWithOffsetRequest
{
    /// <summary>
    /// Gets or sets the desired tracking state.
    /// </summary>
    /// <example>Working</example>
    public required TrackingState State { get; set; }

    /// <summary>
    /// Gets or sets the time offset in minutes.
    /// Positive values mean the action started in the past.
    /// Negative values mean the action will start in the future.
    /// </summary>
    /// <example>-15</example>
    public required int OffsetMinutes { get; set; }
}
