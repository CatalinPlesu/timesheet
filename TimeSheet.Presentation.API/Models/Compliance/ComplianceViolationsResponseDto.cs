namespace TimeSheet.Presentation.API.Models.Compliance;

/// <summary>
/// DTO representing the compliance violations response for a date range.
/// </summary>
public sealed class ComplianceViolationsResponseDto
{
    /// <summary>
    /// Gets or sets the list of compliance violations found within the date range.
    /// Empty when no violations exist or no compliance rules are configured.
    /// </summary>
    public required IReadOnlyList<ComplianceViolationDto> Violations { get; set; }

    /// <summary>
    /// Gets or sets the total number of calendar days in the requested range.
    /// </summary>
    public required int TotalDays { get; set; }

    /// <summary>
    /// Gets or sets the number of violations found.
    /// </summary>
    public required int ViolationCount { get; set; }
}
