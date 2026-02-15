using TimeSheet.Core.Domain.Enums;
using TimeSheet.Core.Domain.SharedKernel;

namespace TimeSheet.Core.Domain.Entities;

/// <summary>
/// Represents a holiday, vacation day, or other non-working day for a user.
/// Holidays are excluded from overtime and target work hour calculations.
/// Supports both single-day entries and multi-day ranges.
/// </summary>
public sealed class Holiday : CreatedEntity
{
    /// <summary>
    /// Gets the ID of the user who owns this holiday.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Gets the start date of the holiday (inclusive).
    /// For single-day holidays, this is the only relevant date.
    /// </summary>
    public DateOnly StartDate { get; init; }

    /// <summary>
    /// Gets the end date of the holiday (inclusive).
    /// For single-day holidays, this equals StartDate.
    /// For multi-day ranges, this is the last day of the holiday.
    /// </summary>
    public DateOnly EndDate { get; init; }

    /// <summary>
    /// Gets the type of holiday (public holiday, vacation, sick leave, etc.).
    /// </summary>
    public HolidayType Type { get; init; }

    /// <summary>
    /// Gets the optional description or reason for the holiday.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets whether this is a single-day holiday.
    /// </summary>
    public bool IsSingleDay => StartDate == EndDate;

    /// <summary>
    /// Gets the number of days this holiday spans (inclusive).
    /// </summary>
    public int DayCount => EndDate.DayNumber - StartDate.DayNumber + 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="Holiday"/> class.
    /// Used when creating a new holiday.
    /// </summary>
    /// <param name="userId">The ID of the user who owns this holiday.</param>
    /// <param name="startDate">The start date of the holiday (inclusive).</param>
    /// <param name="endDate">The end date of the holiday (inclusive).</param>
    /// <param name="type">The type of holiday.</param>
    /// <param name="description">Optional description or reason for the holiday.</param>
    /// <exception cref="ArgumentException">Thrown when endDate is before startDate.</exception>
    public Holiday(
        Guid userId,
        DateOnly startDate,
        DateOnly endDate,
        HolidayType type,
        string? description = null)
        : base()
    {
        if (endDate < startDate)
            throw new ArgumentException("End date cannot be before start date.", nameof(endDate));

        UserId = userId;
        StartDate = startDate;
        EndDate = endDate;
        Type = type;
        Description = description;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Holiday"/> class with a specified ID.
    /// Used for entity rehydration from persistence.
    /// </summary>
    /// <param name="id">The unique identifier for this entity.</param>
    /// <param name="createdAt">The UTC timestamp when this entity was created.</param>
    /// <param name="userId">The ID of the user who owns this holiday.</param>
    /// <param name="startDate">The start date of the holiday (inclusive).</param>
    /// <param name="endDate">The end date of the holiday (inclusive).</param>
    /// <param name="type">The type of holiday.</param>
    /// <param name="description">Optional description or reason for the holiday.</param>
    /// <exception cref="ArgumentException">Thrown when endDate is before startDate.</exception>
    public Holiday(
        Guid id,
        DateTimeOffset createdAt,
        Guid userId,
        DateOnly startDate,
        DateOnly endDate,
        HolidayType type,
        string? description = null)
        : base(id, createdAt)
    {
        if (endDate < startDate)
            throw new ArgumentException("End date cannot be before start date.", nameof(endDate));

        UserId = userId;
        StartDate = startDate;
        EndDate = endDate;
        Type = type;
        Description = description;
    }

    /// <summary>
    /// Helper method to create a single-day holiday.
    /// </summary>
    /// <param name="userId">The ID of the user who owns this holiday.</param>
    /// <param name="date">The date of the holiday.</param>
    /// <param name="type">The type of holiday.</param>
    /// <param name="description">Optional description or reason for the holiday.</param>
    /// <returns>A new Holiday instance.</returns>
    public static Holiday CreateSingleDay(
        Guid userId,
        DateOnly date,
        HolidayType type,
        string? description = null)
    {
        return new Holiday(userId, date, date, type, description);
    }

    /// <summary>
    /// Checks if this holiday includes the specified date.
    /// </summary>
    /// <param name="date">The date to check.</param>
    /// <returns>True if the date falls within this holiday's range (inclusive); otherwise, false.</returns>
    public bool IncludesDate(DateOnly date)
    {
        return date >= StartDate && date <= EndDate;
    }

    /// <summary>
    /// Checks if this holiday overlaps with another holiday.
    /// </summary>
    /// <param name="other">The other holiday to check against.</param>
    /// <returns>True if the holidays overlap; otherwise, false.</returns>
    public bool OverlapsWith(Holiday other)
    {
        return StartDate <= other.EndDate && EndDate >= other.StartDate;
    }
}
