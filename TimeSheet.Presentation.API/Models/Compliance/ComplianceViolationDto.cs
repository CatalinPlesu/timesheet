namespace TimeSheet.Presentation.API.Models.Compliance;

/// <summary>
/// DTO representing a single compliance violation for a specific day.
/// </summary>
public sealed class ComplianceViolationDto
{
    /// <summary>
    /// Gets or sets the date on which the violation occurred.
    /// </summary>
    public required DateOnly Date { get; set; }

    /// <summary>
    /// Gets or sets the type of compliance rule that was violated (e.g., "MinimumSpan").
    /// </summary>
    public required string RuleType { get; set; }

    /// <summary>
    /// Gets or sets the actual number of hours recorded for the evaluated span.
    /// Null when the span could not be determined (e.g., missing clock-in or clock-out data).
    /// </summary>
    public double? ActualHours { get; set; }

    /// <summary>
    /// Gets or sets the minimum number of hours required for compliance.
    /// </summary>
    public required double ThresholdHours { get; set; }

    /// <summary>
    /// Gets or sets a human-readable description of the violation.
    /// </summary>
    public required string Description { get; set; }
}
