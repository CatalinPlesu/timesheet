namespace TimeSheet.Core.Domain.Enums;

/// <summary>
/// Represents the direction of a commute.
/// Used to distinguish between morning and evening commutes for analysis.
/// </summary>
public enum CommuteDirection
{
    /// <summary>
    /// Commute from home to work (typically morning).
    /// </summary>
    ToWork = 0,

    /// <summary>
    /// Commute from work to home (typically evening).
    /// </summary>
    ToHome = 1
}
