using Microsoft.EntityFrameworkCore;
using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.Repositories;

namespace TimeSheet.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for <see cref="UserComplianceRule"/> entity operations.
/// Extends the generic repository with compliance-rule-specific EF Core queries.
/// </summary>
/// <param name="dbContext">The application database context.</param>
public sealed class UserComplianceRuleRepository(AppDbContext dbContext)
    : Repository<UserComplianceRule>(dbContext), IUserComplianceRuleRepository
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<UserComplianceRule>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await DbSet
            .Where(r => r.UserId == userId)
            .ToListAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<UserComplianceRule?> GetByUserAndTypeAsync(Guid userId, string ruleType, CancellationToken ct = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(r => r.UserId == userId && r.RuleType == ruleType, ct);
    }
}
