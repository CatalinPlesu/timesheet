namespace TimeSheet.Presentation.API.Models.Entries;

/// <summary>
/// Request model for updating an entry's time.
/// </summary>
public sealed class EntryUpdateRequest
{
    /// <summary>
    /// Gets or sets the adjustment in minutes.
    /// Positive values extend the session, negative values shorten it.
    /// </summary>
    /// <example>-30</example>
    public required int AdjustmentMinutes { get; set; }
}
