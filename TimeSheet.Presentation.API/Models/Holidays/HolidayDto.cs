using TimeSheet.Core.Domain.Enums;

namespace TimeSheet.Presentation.API.Models.Holidays;

/// <summary>
/// DTO representing a holiday or vacation day.
/// </summary>
public sealed class HolidayDto
{
    /// <summary>
    /// Gets or sets the unique identifier of the holiday.
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the start date of the holiday (inclusive).
    /// </summary>
    public required DateOnly StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date of the holiday (inclusive).
    /// </summary>
    public required DateOnly EndDate { get; set; }

    /// <summary>
    /// Gets or sets the type of holiday.
    /// </summary>
    public required HolidayType Type { get; set; }

    /// <summary>
    /// Gets or sets the optional description or reason for the holiday.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether this is a single-day holiday.
    /// </summary>
    public required bool IsSingleDay { get; set; }

    /// <summary>
    /// Gets or sets the number of days this holiday spans.
    /// </summary>
    public required int DayCount { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when this holiday was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; set; }
}
