using System.ComponentModel.DataAnnotations;
using TimeSheet.Core.Domain.Enums;

namespace TimeSheet.Presentation.API.Models.Holidays;

/// <summary>
/// Request DTO for creating a new holiday.
/// </summary>
public sealed class CreateHolidayRequest
{
    /// <summary>
    /// Gets or sets the start date of the holiday (inclusive).
    /// For single-day holidays, this is the only relevant date.
    /// </summary>
    [Required]
    public required DateOnly StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date of the holiday (inclusive).
    /// For single-day holidays, this should equal StartDate.
    /// For multi-day ranges, this is the last day of the holiday.
    /// </summary>
    [Required]
    public required DateOnly EndDate { get; set; }

    /// <summary>
    /// Gets or sets the type of holiday.
    /// </summary>
    [Required]
    public required HolidayType Type { get; set; }

    /// <summary>
    /// Gets or sets the optional description or reason for the holiday.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }
}
