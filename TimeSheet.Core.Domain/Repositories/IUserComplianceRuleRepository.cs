using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.Interfaces;

namespace TimeSheet.Core.Domain.Repositories;

/// <summary>
/// Repository interface for <see cref="UserComplianceRule"/> entity operations.
/// Provides methods for querying and persisting compliance rules.
/// </summary>
public interface IUserComplianceRuleRepository : IRepository<UserComplianceRule>
{
    /// <summary>
    /// Gets all compliance rules for a specific user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="ct">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A read-only list of all compliance rules for the user.</returns>
    Task<IReadOnlyList<UserComplianceRule>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Gets a specific compliance rule for a user by rule type.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="ruleType">The rule type identifier (e.g., "MinimumSpan").</param>
    /// <param name="ct">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The compliance rule if found; otherwise, null.</returns>
    Task<UserComplianceRule?> GetByUserAndTypeAsync(Guid userId, string ruleType, CancellationToken ct = default);
}
