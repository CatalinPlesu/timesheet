namespace TimeSheet.Presentation.API.Models.Settings;

/// <summary>
/// Request model for updating target hours (work and office).
/// </summary>
public sealed class UpdateTargetHoursRequest
{
    /// <summary>
    /// Gets or sets the target work hours per day.
    /// Null means disable target.
    /// </summary>
    /// <example>8.0</example>
    public decimal? TargetWorkHours { get; set; }

    /// <summary>
    /// Gets or sets the target office hours per day (clock-in to clock-out span).
    /// Null means disable target.
    /// </summary>
    /// <example>9.0</example>
    public decimal? TargetOfficeHours { get; set; }
}
