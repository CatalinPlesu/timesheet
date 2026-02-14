namespace TimeSheet.Presentation.API.Models.Settings;

/// <summary>
/// Request model for updating auto-shutdown limits.
/// </summary>
public sealed class UpdateAutoShutdownRequest
{
    /// <summary>
    /// Gets or sets the maximum allowed hours for work sessions.
    /// Null means no limit.
    /// </summary>
    public decimal? MaxWorkHours { get; set; }

    /// <summary>
    /// Gets or sets the maximum allowed hours for commute sessions.
    /// Null means no limit.
    /// </summary>
    public decimal? MaxCommuteHours { get; set; }

    /// <summary>
    /// Gets or sets the maximum allowed hours for lunch sessions.
    /// Null means no limit.
    /// </summary>
    public decimal? MaxLunchHours { get; set; }
}
