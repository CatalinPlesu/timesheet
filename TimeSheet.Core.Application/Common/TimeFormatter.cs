namespace TimeSheet.Core.Application.Common;

/// <summary>
/// Provides consistent time formatting utilities across the application.
/// All durations should be displayed as hours:minutes (HH:MM or "Xh Ym"), never as decimal hours.
/// </summary>
public static class TimeFormatter
{
    /// <summary>
    /// Formats a duration in hours as "Xh Ym" (e.g., "8h 30m" or "45m").
    /// </summary>
    /// <param name="hours">Duration in decimal hours (e.g., 8.5)</param>
    /// <returns>Formatted string like "8h 30m" or "45m"</returns>
    public static string FormatDuration(decimal hours)
    {
        if (hours < 0)
        {
            return FormatDuration(Math.Abs(hours));
        }

        var h = (int)hours;
        var m = (int)Math.Round((hours - h) * 60);

        // Handle rounding edge case where 59.5 minutes rounds to 60
        if (m >= 60)
        {
            h++;
            m -= 60;
        }

        if (h >= 1)
        {
            return m > 0 ? $"{h}h {m}m" : $"{h}h";
        }

        return $"{m}m";
    }

    /// <summary>
    /// Formats a duration from TimeSpan as "Xh Ym".
    /// </summary>
    public static string FormatDuration(TimeSpan duration)
    {
        var totalHours = (decimal)duration.TotalHours;
        return FormatDuration(totalHours);
    }

    /// <summary>
    /// Formats a duration as HH:MM (e.g., "08:30" or "00:45").
    /// </summary>
    /// <param name="hours">Duration in decimal hours</param>
    /// <returns>Formatted string like "08:30"</returns>
    public static string FormatDurationAsTime(decimal hours)
    {
        if (hours < 0)
        {
            return FormatDurationAsTime(Math.Abs(hours));
        }

        var h = (int)hours;
        var m = (int)Math.Round((hours - h) * 60);

        // Handle rounding edge case
        if (m >= 60)
        {
            h++;
            m -= 60;
        }

        return $"{h:D2}:{m:D2}";
    }

    /// <summary>
    /// Formats a duration from TimeSpan as HH:MM.
    /// </summary>
    public static string FormatDurationAsTime(TimeSpan duration)
    {
        var totalHours = (decimal)duration.TotalHours;
        return FormatDurationAsTime(totalHours);
    }
}
