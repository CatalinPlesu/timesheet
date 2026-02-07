using Microsoft.EntityFrameworkCore;
using TimeSheet.Core.Application.Interfaces;
using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.Enums;

namespace TimeSheet.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for TrackingSession entities with specialized query methods.
/// </summary>
/// <param name="dbContext">The application database context.</param>
public class TrackingSessionRepository(AppDbContext dbContext)
    : Repository<TrackingSession>(dbContext), ITrackingSessionRepository
{
    /// <inheritdoc/>
    public async Task<TrackingSession?> GetActiveSessionAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(s => s.UserId == userId && s.EndedAt == null)
            .OrderByDescending(s => s.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<TrackingSession?> GetLastCommuteSessionAsync(long userId, DateTime date, CancellationToken cancellationToken = default)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        return await DbSet
            .Where(s => s.UserId == userId
                     && s.State == TrackingState.Commuting
                     && s.StartedAt >= startOfDay
                     && s.StartedAt < endOfDay)
            .OrderByDescending(s => s.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> HasWorkedTodayAsync(long userId, DateTime date, CancellationToken cancellationToken = default)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        return await DbSet
            .AnyAsync(s => s.UserId == userId
                        && s.State == TrackingState.Working
                        && s.EndedAt != null
                        && s.StartedAt >= startOfDay
                        && s.StartedAt < endOfDay,
                cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<TrackingSession>> GetRecentSessionsAsync(long userId, int count, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.StartedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<TrackingSession>> GetSessionsByDateAsync(long userId, DateTime date, CancellationToken cancellationToken = default)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        return await DbSet
            .Where(s => s.UserId == userId
                     && s.StartedAt >= startOfDay
                     && s.StartedAt < endOfDay)
            .OrderBy(s => s.StartedAt)
            .ToListAsync(cancellationToken);
    }
}
