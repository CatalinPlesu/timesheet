using System.Text.Json.Serialization;

namespace TimeSheet.Core.Application.ExternalApi.Timily;

/// <summary>
/// Root response object returned by the Timily employer attendance API.
/// </summary>
public record TimilyAttendanceResponse(
    [property: JsonPropertyName("employee")] TimilyEmployee Employee,
    [property: JsonPropertyName("months")] List<TimilyMonth> Months
);

/// <summary>
/// Employee information returned by the Timily API.
/// </summary>
public record TimilyEmployee(
    [property: JsonPropertyName("employeeId")] int EmployeeId,
    [property: JsonPropertyName("firstName")] string FirstName,
    [property: JsonPropertyName("lastName")] string LastName,
    [property: JsonPropertyName("email")] string Email
);

/// <summary>
/// A month of attendance data from the Timily API.
/// </summary>
public record TimilyMonth(
    [property: JsonPropertyName("attendanceDays")] List<TimilyAttendanceDay> AttendanceDays
);

/// <summary>
/// A single day's attendance data from the Timily API.
/// </summary>
public record TimilyAttendanceDay(
    [property: JsonPropertyName("dateOnly")] DateTime DateOnly,
    [property: JsonPropertyName("events")] List<TimilyEvent> Events,
    [property: JsonPropertyName("conflictType")] string ConflictType,
    [property: JsonPropertyName("timeSheetHours")] double TimeSheetHours
);

/// <summary>
/// A single attendance event within a day from the Timily API.
/// </summary>
/// <remarks>
/// Relevant event types:
/// <list type="bullet">
///   <item><description><c>ClockInOutDoor</c> — door clock-in/out event; has meaningful StartDate, EndDate, WorkingHours.</description></item>
///   <item><description><c>Absence</c> — employee was absent.</description></item>
///   <item><description><c>WeekEnd</c> — weekend day.</description></item>
///   <item><description><c>Holiday</c> — public holiday.</description></item>
///   <item><description><c>MinimumWorkedRangeRequirement</c> — minimum hours compliance event.</description></item>
///   <item><description><c>EmployeeNotEmployed</c> — employee was not employed on this day.</description></item>
/// </list>
/// </remarks>
public record TimilyEvent(
    [property: JsonPropertyName("eventType")] string EventType,
    [property: JsonPropertyName("requestId")] int RequestId,
    [property: JsonPropertyName("requestStatus")] string? RequestStatus,
    [property: JsonPropertyName("workingHours")] double WorkingHours,
    [property: JsonPropertyName("eventDescription")] string EventDescription,
    [property: JsonPropertyName("startDate")] DateTime? StartDate,
    [property: JsonPropertyName("endDate")] DateTime? EndDate,
    [property: JsonPropertyName("timeSheetEventLetter")] string TimeSheetEventLetter,
    [property: JsonPropertyName("availableHours")] double? AvailableHours
);
