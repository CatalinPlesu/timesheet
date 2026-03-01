namespace TimeSheet.Presentation.API.Models.Entries;

/// <summary>
/// Request model for creating a completed tracking entry with explicit start and end times.
/// </summary>
public sealed class EntryCreateRequest
{
    /// <summary>
    /// Gets or sets the tracking state for this entry.
    /// Must be one of: Working, Commuting, Lunch.
    /// </summary>
    /// <example>Working</example>
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC start time for the entry.
    /// </summary>
    /// <example>2026-02-25T08:00:00Z</example>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC end time for the entry.
    /// </summary>
    /// <example>2026-02-25T12:00:00Z</example>
    public DateTime EndedAt { get; set; }

    /// <summary>
    /// Gets or sets the commute direction. Required when State is Commuting.
    /// </summary>
    /// <example>ToWork</example>
    public string? CommuteDirection { get; set; }

    /// <summary>
    /// Gets or sets an optional note for this entry.
    /// </summary>
    public string? Note { get; set; }
}
