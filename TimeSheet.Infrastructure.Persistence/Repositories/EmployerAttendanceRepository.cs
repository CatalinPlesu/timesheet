using Microsoft.EntityFrameworkCore;
using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.Repositories;

namespace TimeSheet.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for employer attendance data operations.
/// Provides upsert semantics (insert-or-update) keyed on (UserId, Date).
/// </summary>
/// <param name="dbContext">The application database context.</param>
public sealed class EmployerAttendanceRepository(AppDbContext dbContext) : IEmployerAttendanceRepository
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<EmployerAttendanceRecord>> GetByUserAndDateRangeAsync(
        Guid userId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        return await dbContext.EmployerAttendanceRecords
            .Where(r => r.UserId == userId && r.Date >= from && r.Date <= to)
            .OrderBy(r => r.Date)
            .ToListAsync(ct);
    }

    /// <inheritdoc/>
    public async Task UpsertAsync(EmployerAttendanceRecord record, CancellationToken ct = default)
    {
        var existing = await dbContext.EmployerAttendanceRecords
            .FirstOrDefaultAsync(r => r.UserId == record.UserId && r.Date == record.Date, ct);

        if (existing != null)
        {
            // SetValues copies ALL properties including the PK, which EF Core forbids modifying.
            // Copy only non-key properties by skipping primary key columns.
            var entry = dbContext.Entry(existing);
            var newValues = dbContext.Entry(record);
            foreach (var prop in entry.Properties.Where(p => !p.Metadata.IsPrimaryKey()))
                prop.CurrentValue = newValues.Property(prop.Metadata.Name).CurrentValue;
        }
        else
            await dbContext.EmployerAttendanceRecords.AddAsync(record, ct);
    }

    /// <inheritdoc/>
    public async Task UpsertManyAsync(IEnumerable<EmployerAttendanceRecord> records, CancellationToken ct = default)
    {
        foreach (var record in records)
        {
            await UpsertAsync(record, ct);
        }
    }

    /// <inheritdoc/>
    public async Task<EmployerImportLog?> GetLastImportAsync(Guid userId, CancellationToken ct = default)
    {
        // SQLite doesn't support ORDER BY DateTimeOffset — order by Id (auto-increment) instead
        return await dbContext.EmployerImportLogs
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.Id)
            .FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc/>
    public async Task AddImportLogAsync(EmployerImportLog log, CancellationToken ct = default)
    {
        await dbContext.EmployerImportLogs.AddAsync(log, ct);
    }
}
