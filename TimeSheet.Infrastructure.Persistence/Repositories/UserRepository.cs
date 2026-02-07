using Microsoft.EntityFrameworkCore;
using TimeSheet.Core.Application.Interfaces;
using TimeSheet.Core.Domain.Entities;

namespace TimeSheet.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for User entity operations.
/// Extends the generic repository with User-specific EF Core queries.
/// </summary>
/// <param name="dbContext">The application database context.</param>
public sealed class UserRepository(AppDbContext dbContext) : Repository<User>(dbContext), IUserRepository
{
    /// <inheritdoc/>
    public async Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(u => u.TelegramUserId == telegramUserId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> HasAnyUsersAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(cancellationToken);
    }
}
