using TimeSheet.Core.Domain.SharedKernel;

namespace TimeSheet.Core.Domain.Entities;

/// <summary>
/// Represents an employer work-hour compliance rule for a user.
/// Defines the conditions under which a user's work span is considered compliant.
/// </summary>
/// <remarks>
/// Currently supports the 'MinimumSpan' rule type, which checks that the elapsed time
/// between a clock-in and clock-out event meets or exceeds a threshold.
/// The ClockInDefinition and ClockOutDefinition fields determine which tracking events
/// are used as reference points for the compliance check.
/// </remarks>
public sealed class UserComplianceRule : CreatedEntity
{
    /// <summary>
    /// Gets the ID of the user this rule belongs to.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the rule type identifier.
    /// Currently only "MinimumSpan" is supported.
    /// </summary>
    public string RuleType { get; private set; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether this rule is active.
    /// Disabled rules are stored but not evaluated.
    /// </summary>
    public bool IsEnabled { get; private set; } = true;

    /// <summary>
    /// Gets the minimum number of hours required for compliance.
    /// For example, 9.0 means the span must be at least 9 hours.
    /// </summary>
    public double ThresholdHours { get; private set; }

    /// <summary>
    /// Gets the definition of the clock-in event used as the start reference.
    /// Valid values: "CommuteEnd" (end of first CommuteToWork entry),
    /// "WorkStart" (start of first Work entry), "FixedTime".
    /// </summary>
    public string ClockInDefinition { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the definition of the clock-out event used as the end reference.
    /// Valid values: "CommuteStart" (start of last CommuteToHome entry),
    /// "WorkEnd" (end of last Work entry), "FixedTime".
    /// </summary>
    public string ClockOutDefinition { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the fixed clock-in time used when ClockInDefinition is "FixedTime".
    /// Null when ClockInDefinition is not "FixedTime".
    /// </summary>
    public TimeOnly? FixedClockIn { get; private set; }

    /// <summary>
    /// Gets the fixed clock-out time used when ClockOutDefinition is "FixedTime".
    /// Null when ClockOutDefinition is not "FixedTime".
    /// </summary>
    public TimeOnly? FixedClockOut { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserComplianceRule"/> class.
    /// Used by EF Core for entity materialization.
    /// </summary>
    private UserComplianceRule() { }

    /// <summary>
    /// Creates a new user compliance rule.
    /// </summary>
    /// <param name="userId">The ID of the user this rule belongs to.</param>
    /// <param name="ruleType">The rule type identifier (e.g., "MinimumSpan").</param>
    /// <param name="thresholdHours">The minimum number of hours required for compliance.</param>
    /// <param name="clockInDefinition">The definition of the clock-in event (e.g., "CommuteEnd", "WorkStart", "FixedTime").</param>
    /// <param name="clockOutDefinition">The definition of the clock-out event (e.g., "CommuteStart", "WorkEnd", "FixedTime").</param>
    /// <param name="fixedClockIn">The fixed clock-in time when ClockInDefinition is "FixedTime". Optional.</param>
    /// <param name="fixedClockOut">The fixed clock-out time when ClockOutDefinition is "FixedTime". Optional.</param>
    /// <returns>A new <see cref="UserComplianceRule"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when thresholdHours is not positive.</exception>
    public static UserComplianceRule Create(
        Guid userId,
        string ruleType,
        double thresholdHours,
        string clockInDefinition,
        string clockOutDefinition,
        TimeOnly? fixedClockIn = null,
        TimeOnly? fixedClockOut = null)
    {
        if (thresholdHours <= 0)
            throw new ArgumentException("Threshold hours must be positive.", nameof(thresholdHours));

        return new UserComplianceRule
        {
            UserId = userId,
            RuleType = ruleType,
            ThresholdHours = thresholdHours,
            ClockInDefinition = clockInDefinition,
            ClockOutDefinition = clockOutDefinition,
            FixedClockIn = fixedClockIn,
            FixedClockOut = fixedClockOut,
            IsEnabled = true
        };
    }

    /// <summary>
    /// Updates the rule's threshold, clock definitions, and enabled state.
    /// </summary>
    /// <param name="thresholdHours">The new minimum threshold in hours.</param>
    /// <param name="clockInDefinition">The new clock-in definition.</param>
    /// <param name="clockOutDefinition">The new clock-out definition.</param>
    /// <param name="isEnabled">Whether the rule is enabled.</param>
    /// <param name="fixedClockIn">The fixed clock-in time when ClockInDefinition is "FixedTime". Optional.</param>
    /// <param name="fixedClockOut">The fixed clock-out time when ClockOutDefinition is "FixedTime". Optional.</param>
    /// <exception cref="ArgumentException">Thrown when thresholdHours is not positive.</exception>
    public void Update(
        double thresholdHours,
        string clockInDefinition,
        string clockOutDefinition,
        bool isEnabled,
        TimeOnly? fixedClockIn = null,
        TimeOnly? fixedClockOut = null)
    {
        if (thresholdHours <= 0)
            throw new ArgumentException("Threshold hours must be positive.", nameof(thresholdHours));

        ThresholdHours = thresholdHours;
        ClockInDefinition = clockInDefinition;
        ClockOutDefinition = clockOutDefinition;
        IsEnabled = isEnabled;
        FixedClockIn = fixedClockIn;
        FixedClockOut = fixedClockOut;
    }
}
