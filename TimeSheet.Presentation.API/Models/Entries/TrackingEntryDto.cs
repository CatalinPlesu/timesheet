using TimeSheet.Core.Domain.Enums;

namespace TimeSheet.Presentation.API.Models.Entries;

/// <summary>
/// DTO representing a tracking entry (session).
/// </summary>
public sealed class TrackingEntryDto
{
    /// <summary>
    /// Gets or sets the unique identifier of the entry.
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the tracking state.
    /// </summary>
    public required TrackingState State { get; set; }

    /// <summary>
    /// Gets or sets the start time in UTC.
    /// </summary>
    public required DateTime StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the end time in UTC (null if still active).
    /// </summary>
    public DateTime? EndedAt { get; set; }

    /// <summary>
    /// Gets or sets the duration in hours (null if still active).
    /// </summary>
    public decimal? DurationHours { get; set; }

    /// <summary>
    /// Gets or sets the commute direction (only applicable when State is Commuting).
    /// </summary>
    public CommuteDirection? CommuteDirection { get; set; }

    /// <summary>
    /// Gets or sets whether this entry is currently active.
    /// </summary>
    public required bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets an optional note attached to this entry.
    /// </summary>
    public string? Note { get; set; }
}
