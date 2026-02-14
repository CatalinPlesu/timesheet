namespace TimeSheet.Presentation.API.Models.Entries;

/// <summary>
/// Request model for listing entries with filtering and pagination.
/// </summary>
public sealed class EntryListRequest
{
    /// <summary>
    /// Gets or sets the start date for filtering (UTC, inclusive).
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Gets or sets the end date for filtering (UTC, inclusive).
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Gets or sets the grouping mode for the results.
    /// </summary>
    /// <example>Day</example>
    public GroupingMode? GroupBy { get; set; }

    /// <summary>
    /// Gets or sets the page number (1-based).
    /// </summary>
    /// <example>1</example>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    /// <example>50</example>
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Grouping mode for entry lists.
/// </summary>
public enum GroupingMode
{
    /// <summary>
    /// No grouping, return flat list.
    /// </summary>
    None = 0,

    /// <summary>
    /// Group by day.
    /// </summary>
    Day = 1,

    /// <summary>
    /// Group by week.
    /// </summary>
    Week = 2,

    /// <summary>
    /// Group by month.
    /// </summary>
    Month = 3,

    /// <summary>
    /// Group by year.
    /// </summary>
    Year = 4
}
