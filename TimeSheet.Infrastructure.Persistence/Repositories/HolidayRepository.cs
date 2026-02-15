using Microsoft.EntityFrameworkCore;
using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.Repositories;

namespace TimeSheet.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Holiday entities with specialized query methods.
/// </summary>
/// <param name="dbContext">The application database context.</param>
public class HolidayRepository(AppDbContext dbContext)
    : Repository<Holiday>(dbContext), IHolidayRepository
{
    /// <inheritdoc/>
    public async Task<List<Holiday>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(h => h.UserId == userId)
            .OrderBy(h => h.StartDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<Holiday>> GetByUserIdAndDateRangeAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(h => h.UserId == userId
                     && h.StartDate <= endDate
                     && h.EndDate >= startDate)
            .OrderBy(h => h.StartDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> IsHolidayAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(h => h.UserId == userId
                        && h.StartDate <= date
                        && h.EndDate >= date,
                cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<HashSet<DateOnly>> GetHolidayDatesAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        var holidays = await GetByUserIdAndDateRangeAsync(userId, startDate, endDate, cancellationToken);

        var holidayDates = new HashSet<DateOnly>();

        foreach (var holiday in holidays)
        {
            // Add all dates in the holiday range
            var currentDate = holiday.StartDate > startDate ? holiday.StartDate : startDate;
            var lastDate = holiday.EndDate < endDate ? holiday.EndDate : endDate;

            while (currentDate <= lastDate)
            {
                holidayDates.Add(currentDate);
                currentDate = currentDate.AddDays(1);
            }
        }

        return holidayDates;
    }

    /// <inheritdoc/>
    public async Task<int> CountHolidayDaysAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        var holidayDates = await GetHolidayDatesAsync(userId, startDate, endDate, cancellationToken);
        return holidayDates.Count;
    }

    /// <inheritdoc/>
    public async Task<List<Holiday>> GetUpcomingHolidaysAsync(
        Guid userId,
        DateOnly fromDate,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(h => h.UserId == userId && h.EndDate >= fromDate)
            .OrderBy(h => h.StartDate)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
