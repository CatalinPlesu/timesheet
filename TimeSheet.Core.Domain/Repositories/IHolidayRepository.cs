using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.Interfaces;

namespace TimeSheet.Core.Domain.Repositories;

/// <summary>
/// Repository interface for Holiday entity operations.
/// Provides methods for managing user holidays and querying holiday data.
/// </summary>
public interface IHolidayRepository : IRepository<Holiday>
{
    /// <summary>
    /// Gets all holidays for a specific user, ordered by start date.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A list of all holidays for the user.</returns>
    Task<List<Holiday>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all holidays for a user that fall within a specific date range (inclusive).
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="startDate">The start date of the range (inclusive).</param>
    /// <param name="endDate">The end date of the range (inclusive).</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A list of holidays that overlap with the specified date range.</returns>
    Task<List<Holiday>> GetByUserIdAndDateRangeAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a specific date is marked as a holiday for a user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="date">The date to check.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>True if the date is a holiday; otherwise, false.</returns>
    Task<bool> IsHolidayAsync(Guid userId, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all dates that are marked as holidays for a user within a date range.
    /// Useful for calculating working days vs. holidays in a period.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="startDate">The start date of the range (inclusive).</param>
    /// <param name="endDate">The end date of the range (inclusive).</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A set of dates that are holidays.</returns>
    Task<HashSet<DateOnly>> GetHolidayDatesAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts the number of holiday days for a user within a date range.
    /// Multi-day holidays are counted as separate days.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="startDate">The start date of the range (inclusive).</param>
    /// <param name="endDate">The end date of the range (inclusive).</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The total number of holiday days.</returns>
    Task<int> CountHolidayDaysAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets upcoming holidays for a user (from today onwards), ordered by start date.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="fromDate">The date to start searching from (inclusive).</param>
    /// <param name="limit">Maximum number of holidays to return.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A list of upcoming holidays.</returns>
    Task<List<Holiday>> GetUpcomingHolidaysAsync(
        Guid userId,
        DateOnly fromDate,
        int limit = 10,
        CancellationToken cancellationToken = default);
}
