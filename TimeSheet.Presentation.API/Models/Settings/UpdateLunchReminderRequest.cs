namespace TimeSheet.Presentation.API.Models.Settings;

/// <summary>
/// Request model for updating lunch reminder time.
/// </summary>
public sealed class UpdateLunchReminderRequest
{
    /// <summary>
    /// Gets or sets the hour (0-23) at which to send a lunch reminder.
    /// Null means disable reminder.
    /// </summary>
    /// <example>12</example>
    public int? Hour { get; set; }

    /// <summary>
    /// Gets or sets the minute (0-59) at which to send a lunch reminder.
    /// </summary>
    /// <example>30</example>
    public int Minute { get; set; } = 0;
}
