namespace TimeSheet.Presentation.API.Models.Settings;

/// <summary>
/// Request model for updating target work hours.
/// </summary>
public sealed class UpdateTargetHoursRequest
{
    /// <summary>
    /// Gets or sets the target work hours per day.
    /// Null means disable target.
    /// </summary>
    /// <example>8.0</example>
    public decimal? TargetWorkHours { get; set; }
}
