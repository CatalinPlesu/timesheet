namespace TimeSheet.Presentation.API.Models.Settings;

/// <summary>
/// Request model for updating UTC offset.
/// </summary>
public sealed class UpdateUtcOffsetRequest
{
    /// <summary>
    /// Gets or sets the UTC offset in minutes.
    /// </summary>
    /// <example>60</example>
    public required int UtcOffsetMinutes { get; set; }
}
