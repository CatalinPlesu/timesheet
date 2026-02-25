namespace TimeSheet.Presentation.API.Models.Entries;

/// <summary>
/// Request model for updating an entry's time and/or note.
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

    /// <summary>
    /// Gets or sets the note text for this entry.
    /// Set to null or empty string to clear the note.
    /// When this property is absent from the request body, the note is left unchanged.
    /// When present (even as null/empty), the note is updated.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Gets or sets whether the Note field was explicitly provided in the request.
    /// This allows distinguishing "note not provided" from "note set to null".
    /// </summary>
    public bool UpdateNote { get; set; }
}
