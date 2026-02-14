using TimeSheet.Core.Domain.Enums;

namespace TimeSheet.Presentation.API.Models.Tracking;

/// <summary>
/// Request model for toggling tracking state.
/// </summary>
public sealed class TrackingStateRequest
{
    /// <summary>
    /// Gets or sets the desired tracking state.
    /// </summary>
    /// <example>Working</example>
    public required TrackingState State { get; set; }
}
