namespace TimeSheet.Core.Domain.Enums;

/// <summary>
/// Represents the possible states a user can be in while tracking their time.
/// States are mutually exclusive - a user can only be in one state at a time.
/// </summary>
public enum TrackingState
{
    /// <summary>
    /// User is not actively tracking any activity.
    /// </summary>
    Idle = 0,

    /// <summary>
    /// User is commuting to or from work.
    /// </summary>
    Commuting = 1,

    /// <summary>
    /// User is actively working.
    /// </summary>
    Working = 2,

    /// <summary>
    /// User is on lunch break.
    /// </summary>
    Lunch = 3
}
