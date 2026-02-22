namespace TimeSheet.Core.Application.Models;

/// <summary>
/// Represents a compliance violation for a specific day, where the user's office span
/// did not meet the employer's minimum hours requirement.
/// </summary>
/// <param name="Date">The date on which the violation occurred.</param>
/// <param name="RuleType">The type of compliance rule that was violated (e.g., "MinimumSpan").</param>
/// <param name="ActualHours">
/// The actual number of hours recorded for the evaluated span.
/// Null when the span could not be determined (e.g., missing clock-in or clock-out data).
/// </param>
/// <param name="ThresholdHours">The minimum number of hours required for compliance.</param>
/// <param name="Description">A human-readable description of the violation.</param>
public record ComplianceViolation(
    DateOnly Date,
    string RuleType,
    double? ActualHours,
    double ThresholdHours,
    string Description
);
