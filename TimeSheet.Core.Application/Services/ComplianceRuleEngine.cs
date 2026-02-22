using TimeSheet.Core.Application.Interfaces.Services;
using TimeSheet.Core.Application.Models;
using TimeSheet.Core.Domain.ComplianceRules;
using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.Enums;
using TimeSheet.Core.Domain.Repositories;

namespace TimeSheet.Core.Application.Services;

/// <summary>
/// Evaluates employer compliance rules against a user's tracked time data.
/// Currently supports the MinimumSpan rule type, which verifies that the elapsed time
/// between a user's clock-in and clock-out events meets a minimum threshold.
/// </summary>
public sealed class ComplianceRuleEngine(
    IUserComplianceRuleRepository complianceRuleRepository,
    ITrackingSessionRepository trackingSessionRepository,
    IUserRepository userRepository) : IComplianceRuleEngine
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<ComplianceViolation>> EvaluateAsync(
        Guid userId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default)
    {
        // Load the user to get their Telegram user ID (needed for session queries)
        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user is null)
            return [];

        // Load all enabled compliance rules for this user
        var rules = await complianceRuleRepository.GetByUserIdAsync(userId, ct);
        var minimumSpanRules = rules
            .Where(r => r.IsEnabled && r.RuleType == "MinimumSpan")
            .ToList();

        if (minimumSpanRules.Count == 0)
            return [];

        // Load all sessions in the date range
        var startDateTime = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endDateTime = to.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(1); // exclusive upper bound

        var sessions = await trackingSessionRepository.GetSessionsInRangeAsync(
            user.TelegramUserId,
            startDateTime,
            endDateTime,
            ct);

        if (sessions.Count == 0)
            return [];

        // Group sessions by calendar date (UTC date of StartedAt)
        var sessionsByDate = sessions
            .GroupBy(s => DateOnly.FromDateTime(s.StartedAt))
            .ToDictionary(g => g.Key, g => g.ToList());

        var violations = new List<ComplianceViolation>();

        foreach (var (date, daySessions) in sessionsByDate)
        {
            foreach (var rule in minimumSpanRules)
            {
                var violation = EvaluateMinimumSpanRule(date, daySessions, rule);
                if (violation is not null)
                    violations.Add(violation);
            }
        }

        return violations
            .OrderBy(v => v.Date)
            .ToList();
    }

    /// <summary>
    /// Evaluates a single MinimumSpan rule against one day's sessions.
    /// Returns a <see cref="ComplianceViolation"/> if the span is below the threshold,
    /// or null if the day is compliant or data is insufficient.
    /// </summary>
    private static ComplianceViolation? EvaluateMinimumSpanRule(
        DateOnly date,
        List<TrackingSession> daySessions,
        UserComplianceRule rule)
    {
        var clockIn = ResolveClockIn(daySessions, rule.ClockInDefinition);
        var clockOut = ResolveClockOut(daySessions, rule.ClockOutDefinition);

        // Skip days where either reference point cannot be determined
        if (clockIn is null || clockOut is null)
            return null;

        // Ensure clock-out is after clock-in (guard against data anomalies)
        if (clockOut <= clockIn)
            return null;

        var spanHours = (clockOut.Value - clockIn.Value).TotalHours;

        if (spanHours >= rule.ThresholdHours)
            return null; // Compliant

        return new ComplianceViolation(
            Date: date,
            RuleType: rule.RuleType,
            ActualHours: spanHours,
            ThresholdHours: rule.ThresholdHours,
            Description: $"Office span of {spanHours:F2}h on {date:yyyy-MM-dd} is below the required {rule.ThresholdHours:F2}h " +
                         $"(measured from {rule.ClockInDefinition} to {rule.ClockOutDefinition})."
        );
    }

    /// <summary>
    /// Resolves the clock-in DateTime for a day based on the rule's ClockInDefinition.
    /// Returns null if the required event is not found in the day's sessions.
    /// </summary>
    private static DateTime? ResolveClockIn(List<TrackingSession> daySessions, string clockInDefinition)
    {
        return clockInDefinition switch
        {
            ClockDefinition.CommuteEnd =>
                // End of the first CommuteToWork entry (arrival at office)
                daySessions
                    .Where(s => s.State == TrackingState.Commuting
                             && s.CommuteDirection == CommuteDirection.ToWork
                             && s.EndedAt.HasValue)
                    .OrderBy(s => s.StartedAt)
                    .FirstOrDefault()
                    ?.EndedAt,

            ClockDefinition.WorkStart =>
                // Start of the first Work entry of the day
                daySessions
                    .Where(s => s.State == TrackingState.Working)
                    .OrderBy(s => s.StartedAt)
                    .FirstOrDefault()
                    ?.StartedAt,

            _ => null
        };
    }

    /// <summary>
    /// Resolves the clock-out DateTime for a day based on the rule's ClockOutDefinition.
    /// Returns null if the required event is not found or the session is still active.
    /// </summary>
    private static DateTime? ResolveClockOut(List<TrackingSession> daySessions, string clockOutDefinition)
    {
        return clockOutDefinition switch
        {
            ClockDefinition.CommuteStart =>
                // Start of the last CommuteToHome entry (departure from office)
                daySessions
                    .Where(s => s.State == TrackingState.Commuting
                             && s.CommuteDirection == CommuteDirection.ToHome)
                    .OrderByDescending(s => s.StartedAt)
                    .FirstOrDefault()
                    ?.StartedAt,

            ClockDefinition.WorkEnd =>
                // End of the last Work entry of the day (must be a completed session)
                daySessions
                    .Where(s => s.State == TrackingState.Working && s.EndedAt.HasValue)
                    .OrderByDescending(s => s.StartedAt)
                    .FirstOrDefault()
                    ?.EndedAt,

            _ => null
        };
    }
}
