namespace TimeSheet.Presentation.API.Models.Settings;

/// <summary>
/// Request model for updating forgot-shutdown threshold.
/// </summary>
public sealed class UpdateForgotThresholdRequest
{
    /// <summary>
    /// Gets or sets the threshold percentage for forgot-to-shutdown detection.
    /// Null means disable detection.
    /// </summary>
    /// <example>150</example>
    public int? ThresholdPercent { get; set; }
}
