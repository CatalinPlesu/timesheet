namespace TimeSheet.Presentation.API.Models.Entries;

/// <summary>
/// Request model for updating an entry's time.
/// </summary>
public sealed class EntryUpdateRequest
{
    /// <summary>
    /// Gets or sets the start time adjustment in minutes.
    /// Positive values move the start time later, negative values move it earlier.
    /// </summary>
    /// <example>-5</example>
    public int? StartAdjustmentMinutes { get; set; }

    /// <summary>
    /// Gets or sets the end time adjustment in minutes.
    /// Positive values extend the session, negative values shorten it.
    /// </summary>
    /// <example>-30</example>
    public int? EndAdjustmentMinutes { get; set; }
}
