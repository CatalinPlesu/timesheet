namespace TimeSheet.Presentation.API.Models.Settings;

/// <summary>
/// Request model for updating all user settings in a single request.
/// </summary>
public sealed class UpdateAllSettingsRequest
{
    /// <summary>
    /// Gets or sets the user's UTC offset in minutes.
    /// </summary>
    /// <example>60</example>
    public required int UtcOffsetMinutes { get; set; }

    /// <summary>
    /// Gets or sets the maximum allowed hours for a work session before auto-shutdown.
    /// Null means no limit.
    /// </summary>
    public decimal? MaxWorkHours { get; set; }

    /// <summary>
    /// Gets or sets the maximum allowed hours for a commute session before auto-shutdown.
    /// Null means no limit.
    /// </summary>
    public decimal? MaxCommuteHours { get; set; }

    /// <summary>
    /// Gets or sets the maximum allowed hours for a lunch session before auto-shutdown.
    /// Null means no limit.
    /// </summary>
    public decimal? MaxLunchHours { get; set; }

    /// <summary>
    /// Gets or sets the hour (0-23) at which to send a lunch reminder.
    /// Null means no lunch reminder is configured.
    /// </summary>
    public int? LunchReminderHour { get; set; }

    /// <summary>
    /// Gets or sets the minute (0-59) at which to send a lunch reminder.
    /// </summary>
    public int LunchReminderMinute { get; set; } = 0;

    /// <summary>
    /// Gets or sets the target work hours per day.
    /// Null means no target is configured.
    /// </summary>
    public decimal? TargetWorkHours { get; set; }

    /// <summary>
    /// Gets or sets the target office hours per day (clock-in to clock-out span).
    /// Null means no target is configured.
    /// </summary>
    public decimal? TargetOfficeHours { get; set; }

    /// <summary>
    /// Gets or sets the threshold percentage for forgot-to-shutdown detection.
    /// Must be greater than 100. Null means no detection.
    /// </summary>
    public int? ForgotShutdownThresholdPercent { get; set; }
}
