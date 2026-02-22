namespace TimeSheet.Presentation.API.Models.EmployerAttendance;

/// <summary>
/// DTO representing the employer attendance response for a date range.
/// </summary>
public sealed class EmployerAttendanceResponseDto
{
    /// <summary>
    /// Gets or sets the list of employer attendance records within the requested date range.
    /// Empty when no records exist for the range.
    /// </summary>
    public required IReadOnlyList<EmployerAttendanceRecordDto> Records { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp of the most recent import run for the user.
    /// Null when no imports have been performed yet.
    /// </summary>
    public DateTimeOffset? LastImport { get; set; }

    /// <summary>
    /// Gets or sets the total number of records returned.
    /// </summary>
    public required int TotalRecords { get; set; }
}
