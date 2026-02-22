namespace TimeSheet.Presentation.API.Models.EmployerAttendance;

/// <summary>
/// DTO representing a single employer attendance record (door clock-in/out data for one day).
/// </summary>
public sealed class EmployerAttendanceRecordDto
{
    /// <summary>
    /// Gets or sets the calendar date this record applies to.
    /// </summary>
    public required DateOnly Date { get; set; }

    /// <summary>
    /// Gets or sets the door clock-in timestamp.
    /// Null when there is no clock-in event for the day (e.g., absence, weekend).
    /// </summary>
    public DateTime? ClockIn { get; set; }

    /// <summary>
    /// Gets or sets the door clock-out timestamp.
    /// Null when there is no clock-out event for the day.
    /// </summary>
    public DateTime? ClockOut { get; set; }

    /// <summary>
    /// Gets or sets the door-to-door working hours span as reported by the employer.
    /// Null when there is no clock-in/out event.
    /// </summary>
    public double? WorkingHours { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the employer flagged a conflict for this day.
    /// </summary>
    public required bool HasConflict { get; set; }

    /// <summary>
    /// Gets or sets the conflict type string from the employer (e.g., "ConflictGeneral").
    /// Null when there is no conflict.
    /// </summary>
    public string? ConflictType { get; set; }

    /// <summary>
    /// Gets or sets the comma-separated list of event type identifiers present on this day
    /// (e.g., "ClockInOutDoor", "MinimumWorkedRangeRequirement").
    /// </summary>
    public required string EventTypes { get; set; }
}
