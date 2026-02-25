namespace TimeSheet.Presentation.API.Models.Entries;

/// <summary>
/// Request model for updating the note on a tracking entry.
/// </summary>
public sealed class EntryNoteRequest
{
    /// <summary>
    /// Gets or sets the note text. Set to null or empty string to clear the note.
    /// </summary>
    /// <example>Meeting with team about Q2 planning</example>
    public string? Note { get; set; }
}
