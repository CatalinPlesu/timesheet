namespace TimeSheet.Core.Application.Interfaces;

using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.Interfaces;

/// <summary>
/// Repository interface for User entity operations.
/// Extends the generic repository with User-specific queries.
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// Retrieves a user by their Telegram user ID.
    /// </summary>
    /// <param name="telegramUserId">The Telegram user ID to search for.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The user if found; otherwise, null.</returns>
    Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether any users exist in the system.
    /// Used during registration to determine if the registering user should be granted admin privileges.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>True if at least one user exists; otherwise, false.</returns>
    Task<bool> HasAnyUsersAsync(CancellationToken cancellationToken = default);
}
