using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.Interfaces;

namespace TimeSheet.Core.Domain.Repositories;

/// <summary>
/// Repository interface for PendingMnemonic entity operations.
/// Extends the generic repository with PendingMnemonic-specific queries.
/// </summary>
public interface IPendingMnemonicRepository : IRepository<PendingMnemonic>
{
    /// <summary>
    /// Finds a pending mnemonic by its mnemonic string.
    /// </summary>
    /// <param name="mnemonic">The mnemonic string to search for.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The pending mnemonic if found; otherwise, null.</returns>
    Task<PendingMnemonic?> FindByMnemonicAsync(string mnemonic, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all expired mnemonics from the database.
    /// A mnemonic is considered expired if its ExpiresAt timestamp is in the past.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The number of expired mnemonics that were deleted.</returns>
    Task<int> DeleteExpiredAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all consumed mnemonics from the database.
    /// This is useful for cleanup to prevent the table from growing indefinitely.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The number of consumed mnemonics that were deleted.</returns>
    Task<int> DeleteConsumedAsync(CancellationToken cancellationToken = default);
}
