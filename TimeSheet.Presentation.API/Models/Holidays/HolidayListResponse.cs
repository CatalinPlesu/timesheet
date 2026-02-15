namespace TimeSheet.Presentation.API.Models.Holidays;

/// <summary>
/// Response DTO containing a list of holidays.
/// </summary>
public sealed class HolidayListResponse
{
    /// <summary>
    /// Gets or sets the list of holidays.
    /// </summary>
    public required List<HolidayDto> Holidays { get; set; }

    /// <summary>
    /// Gets or sets the total count of holidays.
    /// </summary>
    public required int TotalCount { get; set; }
}
