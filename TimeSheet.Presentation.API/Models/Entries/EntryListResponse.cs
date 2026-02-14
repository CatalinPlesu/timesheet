namespace TimeSheet.Presentation.API.Models.Entries;

/// <summary>
/// Response model for entry list.
/// </summary>
public sealed class EntryListResponse
{
    /// <summary>
    /// Gets or sets the list of entries.
    /// </summary>
    public required List<TrackingEntryDto> Entries { get; set; }

    /// <summary>
    /// Gets or sets the total count of entries matching the filter.
    /// </summary>
    public required int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the current page number.
    /// </summary>
    public required int Page { get; set; }

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public required int PageSize { get; set; }

    /// <summary>
    /// Gets or sets the total number of pages.
    /// </summary>
    public required int TotalPages { get; set; }
}
