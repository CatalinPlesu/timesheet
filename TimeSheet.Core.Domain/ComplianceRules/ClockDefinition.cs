namespace TimeSheet.Core.Domain.ComplianceRules;

/// <summary>
/// Defines the string constants used to identify clock-in and clock-out events
/// for compliance rule evaluation.
/// </summary>
public static class ClockDefinition
{
    /// <summary>
    /// Clock-in event: end of the first CommuteToWork entry of the day.
    /// Represents the moment the user arrives at the office.
    /// </summary>
    public const string CommuteEnd = "CommuteEnd";

    /// <summary>
    /// Clock-in event: start of the first Work entry of the day.
    /// Represents the moment the user starts working.
    /// </summary>
    public const string WorkStart = "WorkStart";

    /// <summary>
    /// Clock-out event: start of the last CommuteToHome entry of the day.
    /// Represents the moment the user leaves the office to go home.
    /// </summary>
    public const string CommuteStart = "CommuteStart";

    /// <summary>
    /// Clock-out event: end of the last Work entry of the day.
    /// Represents the moment the user stops working.
    /// </summary>
    public const string WorkEnd = "WorkEnd";
}
