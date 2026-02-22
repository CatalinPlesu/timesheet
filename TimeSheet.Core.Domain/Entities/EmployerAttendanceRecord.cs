using TimeSheet.Core.Domain.SharedKernel;

namespace TimeSheet.Core.Domain.Entities;

/// <summary>
/// Represents an employer's door clock-in/out attendance record for a user on a given day.
/// Data is imported from the employer's attendance API (e.g., Timily).
/// </summary>
/// <remarks>
/// The employer API returns daily attendance events. The relevant event type is
/// <c>ClockInOutDoor</c>, which provides the door-to-door span (clock-in and clock-out timestamps).
/// Other event types (Absence, WeekEnd, Holiday, MinimumWorkedRangeRequirement) are stored
/// as a comma-separated list in <see cref="EventTypes"/> for diagnostic purposes.
/// </remarks>
public sealed class EmployerAttendanceRecord : CreatedEntity
{
    /// <summary>
    /// Gets the ID of the user this record belongs to.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the calendar date this record applies to.
    /// </summary>
    public DateOnly Date { get; private set; }

    /// <summary>
    /// Gets the door clock-in timestamp (from <c>ClockInOutDoor.startDate</c>).
    /// Null when there is no clock-in event for the day (e.g., absence, weekend).
    /// </summary>
    public DateTime? ClockIn { get; private set; }

    /// <summary>
    /// Gets the door clock-out timestamp (from <c>ClockInOutDoor.endDate</c>).
    /// Null when there is no clock-out event for the day.
    /// </summary>
    public DateTime? ClockOut { get; private set; }

    /// <summary>
    /// Gets the door-to-door working hours span as reported by the employer API
    /// (from <c>ClockInOutDoor.workingHours</c>).
    /// Null when there is no clock-in/out event.
    /// </summary>
    public double? WorkingHours { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the employer API flagged a conflict for this day.
    /// True when the parent day's <c>conflictType</c> is not "None".
    /// </summary>
    public bool HasConflict { get; private set; }

    /// <summary>
    /// Gets the conflict type string from the employer API (e.g., "ConflictGeneral").
    /// Null when there is no conflict.
    /// </summary>
    public string? ConflictType { get; private set; }

    /// <summary>
    /// Gets the comma-separated list of event type identifiers present on this day
    /// (e.g., "ClockInOutDoor", "MinimumWorkedRangeRequirement").
    /// </summary>
    public string EventTypes { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the import batch ID (a GUID string) identifying which import run created this record.
    /// Enables tracing records back to a specific import operation.
    /// </summary>
    public string ImportBatchId { get; private set; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmployerAttendanceRecord"/> class.
    /// Used by EF Core for entity materialization.
    /// </summary>
    private EmployerAttendanceRecord() { }

    /// <summary>
    /// Creates a new employer attendance record.
    /// </summary>
    /// <param name="userId">The ID of the user this record belongs to.</param>
    /// <param name="date">The calendar date for this record.</param>
    /// <param name="clockIn">The door clock-in timestamp, or null if not present.</param>
    /// <param name="clockOut">The door clock-out timestamp, or null if not present.</param>
    /// <param name="workingHours">The door-to-door working hours span, or null if not present.</param>
    /// <param name="hasConflict">Whether the employer API flagged a conflict for this day.</param>
    /// <param name="conflictType">The conflict type string, or null if no conflict.</param>
    /// <param name="eventTypes">Comma-separated event type identifiers for this day.</param>
    /// <param name="importBatchId">The GUID string identifying the import run.</param>
    /// <returns>A new <see cref="EmployerAttendanceRecord"/> instance.</returns>
    public static EmployerAttendanceRecord Create(
        Guid userId,
        DateOnly date,
        DateTime? clockIn,
        DateTime? clockOut,
        double? workingHours,
        bool hasConflict,
        string? conflictType,
        string eventTypes,
        string importBatchId)
    {
        return new EmployerAttendanceRecord
        {
            UserId = userId,
            Date = date,
            ClockIn = clockIn,
            ClockOut = clockOut,
            WorkingHours = workingHours,
            HasConflict = hasConflict,
            ConflictType = conflictType,
            EventTypes = eventTypes,
            ImportBatchId = importBatchId
        };
    }
}
