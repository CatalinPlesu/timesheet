namespace TimeSheet.Presentation.API.Models.Entries;

/// <summary>
/// Request model for setting the absolute start and/or end time of an entry.
/// Used for direct time editing (HH:MM picker) rather than ±minute adjustments.
/// </summary>
public sealed class EntrySetTimesRequest
{
    /// <summary>
    /// Gets or sets the new UTC start time. Null to leave unchanged.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the new UTC end time. Null to leave unchanged.
    /// </summary>
    public DateTime? EndedAt { get; set; }
}
