using TimeSheet.Core.Application.Models;

namespace TimeSheet.Core.Application.Interfaces.Services;

/// <summary>
/// Evaluates compliance rules for a user's time tracking data over a date range.
/// </summary>
public interface IComplianceRuleEngine
{
    /// <summary>
    /// Evaluates all enabled compliance rules for a user within the specified date range
    /// and returns any violations found.
    /// </summary>
    /// <param name="userId">The entity ID (Guid) of the user whose rules should be evaluated.</param>
    /// <param name="from">The start of the date range to evaluate (inclusive).</param>
    /// <param name="to">The end of the date range to evaluate (inclusive).</param>
    /// <param name="ct">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A read-only list of <see cref="ComplianceViolation"/> instances representing all days
    /// where the user's tracked data did not meet the configured compliance requirements.
    /// Returns an empty list when no violations are found.
    /// </returns>
    Task<IReadOnlyList<ComplianceViolation>> EvaluateAsync(
        Guid userId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default);
}
