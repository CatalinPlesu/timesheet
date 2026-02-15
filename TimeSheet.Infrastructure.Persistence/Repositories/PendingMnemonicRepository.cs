using Microsoft.EntityFrameworkCore;
using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.Repositories;

namespace TimeSheet.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for PendingMnemonic entity operations.
/// Extends the generic repository with PendingMnemonic-specific EF Core queries.
/// </summary>
/// <param name="dbContext">The application database context.</param>
public sealed class PendingMnemonicRepository(AppDbContext dbContext)
    : Repository<PendingMnemonic>(dbContext), IPendingMnemonicRepository
{
    private readonly AppDbContext _dbContext = dbContext;

    /// <inheritdoc/>
    public async Task<PendingMnemonic?> FindByMnemonicAsync(string mnemonic, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(m => m.Mnemonic == mnemonic, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> DeleteExpiredAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        return await DbSet
            .Where(m => m.ExpiresAt <= now)
            .ExecuteDeleteAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> DeleteConsumedAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(m => m.IsConsumed)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
