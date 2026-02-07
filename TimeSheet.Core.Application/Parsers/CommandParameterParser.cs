using System.Text.RegularExpressions;

namespace TimeSheet.Core.Application.Parsers;

/// <summary>
/// Parses optional time parameters from tracking commands.
/// Supports: -/+minutes for offsets, [HH:MM] or HH:MM for explicit times.
/// </summary>
public partial class CommandParameterParser : ICommandParameterParser
{
    private const int MaxMinuteOffset = 720; // ±12 hours

    // Regex patterns for parameter formats
    [GeneratedRegex(@"[-+]m?\s*(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex MinuteOffsetRegex();

    [GeneratedRegex(@"\[?(\d{1,2}):(\d{2})\]?")]
    private static partial Regex ExplicitTimeRegex();

    /// <inheritdoc />
    public DateTime ParseTimestamp(string commandText, int userUtcOffsetMinutes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandText);

        var now = DateTime.UtcNow;
        var userLocalNow = now.AddMinutes(userUtcOffsetMinutes);

        // Extract parameter from command text (everything after the command)
        var parts = commandText.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // If only the command, use current time
        if (parts.Length == 1)
        {
            return now;
        }

        var parameter = string.Join(' ', parts.Skip(1));

        // Try minute offset first (-15, +30, -m 15, +m 30)
        var minuteOffsetMatch = MinuteOffsetRegex().Match(parameter);
        if (minuteOffsetMatch.Success)
        {
            return ParseMinuteOffset(parameter, minuteOffsetMatch, now);
        }

        // Try explicit time ([14:30] or 14:30)
        var explicitTimeMatch = ExplicitTimeRegex().Match(parameter);
        if (explicitTimeMatch.Success)
        {
            return ParseExplicitTime(explicitTimeMatch, userLocalNow, userUtcOffsetMinutes);
        }

        throw new ArgumentException(
            $"Invalid time parameter format: '{parameter}'. " +
            "Supported formats: -/+minutes (e.g., -15, +30) or [HH:MM] (e.g., [14:30], 09:00)");
    }

    private static DateTime ParseMinuteOffset(string parameter, Match match, DateTime now)
    {
        var minutes = int.Parse(match.Groups[1].Value);

        if (minutes > MaxMinuteOffset)
        {
            throw new ArgumentException(
                $"Minute offset too large: {minutes}. Maximum allowed is ±{MaxMinuteOffset} minutes (±12 hours).");
        }

        // Determine if it's positive or negative
        var isNegative = parameter.TrimStart().StartsWith('-');
        var offset = isNegative ? -minutes : minutes;

        return now.AddMinutes(offset);
    }

    private static DateTime ParseExplicitTime(Match match, DateTime userLocalNow, int userUtcOffsetMinutes)
    {
        var hour = int.Parse(match.Groups[1].Value);
        var minute = int.Parse(match.Groups[2].Value);

        if (hour < 0 || hour > 23)
        {
            throw new ArgumentException($"Invalid hour: {hour}. Must be between 0 and 23.");
        }

        if (minute < 0 || minute > 59)
        {
            throw new ArgumentException($"Invalid minute: {minute}. Must be between 0 and 59.");
        }

        // Construct the time in user's local time
        var userLocalTime = new DateTime(
            userLocalNow.Year,
            userLocalNow.Month,
            userLocalNow.Day,
            hour,
            minute,
            0,
            DateTimeKind.Unspecified);

        // Convert back to UTC by subtracting the user's offset
        return userLocalTime.AddMinutes(-userUtcOffsetMinutes);
    }
}
