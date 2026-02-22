using TimeSheet.Core.Domain.Entities;

namespace TimeSheet.Core.Domain.Repositories;

/// <summary>
/// Repository interface for <see cref="EmployerAttendanceRecord"/> and <see cref="EmployerImportLog"/> operations.
/// Provides methods for querying and persisting employer attendance data.
/// </summary>
public interface IEmployerAttendanceRepository
{
    /// <summary>
    /// Gets all employer attendance records for a user within the specified date range (inclusive).
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="from">The start date of the range (inclusive).</param>
    /// <param name="to">The end date of the range (inclusive).</param>
    /// <param name="ct">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A read-only list of attendance records ordered by date.</returns>
    Task<IReadOnlyList<EmployerAttendanceRecord>> GetByUserAndDateRangeAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>
    /// Inserts or updates a single employer attendance record.
    /// Matches on (UserId, Date) — the unique key for this entity.
    /// </summary>
    /// <param name="record">The attendance record to upsert.</param>
    /// <param name="ct">A cancellation token to observe while waiting for the task to complete.</param>
    Task UpsertAsync(EmployerAttendanceRecord record, CancellationToken ct = default);

    /// <summary>
    /// Inserts or updates multiple employer attendance records in a single operation.
    /// Matches each record on (UserId, Date).
    /// </summary>
    /// <param name="records">The attendance records to upsert.</param>
    /// <param name="ct">A cancellation token to observe while waiting for the task to complete.</param>
    Task UpsertManyAsync(IEnumerable<EmployerAttendanceRecord> records, CancellationToken ct = default);

    /// <summary>
    /// Gets the most recent import log entry for the specified user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="ct">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The most recent <see cref="EmployerImportLog"/> for the user, or null if no imports have been performed.</returns>
    Task<EmployerImportLog?> GetLastImportAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Adds a new import log entry.
    /// </summary>
    /// <param name="log">The import log entry to add.</param>
    /// <param name="ct">A cancellation token to observe while waiting for the task to complete.</param>
    Task AddImportLogAsync(EmployerImportLog log, CancellationToken ct = default);
}
