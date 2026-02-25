using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TimeSheet.Core.Application.Interfaces.Services;
using TimeSheet.Core.Application.Interfaces.Persistence;
using TimeSheet.Core.Domain.Repositories;
using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.Enums;

namespace TimeSheet.Presentation.Telegram.Handlers;

/// <summary>
/// Handles the /list command to show today's time entries.
/// </summary>
public class ListCommandHandler(
    ILogger<ListCommandHandler> logger,
    IServiceScopeFactory serviceScopeFactory)
{
    // Column widths for monospace alignment
    private const int LabelWidth = 17;
    private const int TimeRangeWidth = 14; // "HH:mm → HH:mm" = 14 chars
    private const int DurationWidth = 7;   // " 1h 30m"
    private const string Separator = "────────────────────────────────────────";

    /// <summary>
    /// Handles the /list command.
    /// </summary>
    public async Task HandleListAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        var userId = message.From?.Id;
        if (userId == null)
        {
            logger.LogWarning("Received /list message without user ID");
            return;
        }

        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var timeTrackingService = scope.ServiceProvider.GetRequiredService<ITimeTrackingService>();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

            // Get user for timezone offset
            var user = await userRepository.GetByTelegramUserIdAsync(userId.Value, cancellationToken);
            if (user == null)
            {
                logger.LogWarning("User {UserId} not found", userId.Value);
                return;
            }

            // Get today's date in UTC
            var today = DateTime.UtcNow.Date;

            // Get all sessions for today
            var sessions = await timeTrackingService.GetTodaysSessionsAsync(
                userId.Value,
                today,
                cancellationToken);

            // Format the response
            var responseText = FormatSessionsList(sessions, today, user.UtcOffsetMinutes);

            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: responseText,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "User {UserId} listed {Count} sessions for today",
                userId.Value,
                sessions.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling /list command for user {UserId}", userId.Value);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "An error occurred while retrieving your tracking entries. Please try again.",
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Formats the list of sessions for display using a flat column-aligned monospace layout.
    /// </summary>
    internal static string FormatSessionsList(List<TrackingSession> sessions, DateTime date, int utcOffsetMinutes)
    {
        if (sessions.Count == 0)
        {
            return "No tracking entries for today.";
        }

        var builder = new StringBuilder();
        builder.AppendLine($"Today's entries ({date:yyyy-MM-dd}):");
        builder.AppendLine("<pre>");

        // --- Session rows ---
        foreach (var session in sessions)
        {
            var label = FormatSessionLabel(session).PadRight(LabelWidth);

            string timeRange;
            if (session.EndedAt.HasValue)
            {
                var startStr = FormatTime(session.StartedAt, utcOffsetMinutes);
                var endStr = FormatTime(session.EndedAt.Value, utcOffsetMinutes);
                timeRange = $"{startStr} → {endStr}";
            }
            else
            {
                var startStr = FormatTime(session.StartedAt, utcOffsetMinutes);
                timeRange = $"{startStr} → ...";
            }

            var duration = session.EndedAt.HasValue
                ? FormatDurationAligned(session.StartedAt, session.EndedAt.Value)
                : "ongoing";

            builder.AppendLine($"{label} {timeRange}  {duration}");
        }

        // --- Separator ---
        builder.AppendLine(Separator);

        // --- Totals ---
        var workSessions = sessions.Where(s => s.State == TrackingState.Working && s.EndedAt.HasValue).ToList();
        var commuteToWorkSessions = sessions.Where(s => s.State == TrackingState.Commuting && s.CommuteDirection == CommuteDirection.ToWork && s.EndedAt.HasValue).ToList();
        var commuteToHomeSessions = sessions.Where(s => s.State == TrackingState.Commuting && s.CommuteDirection == CommuteDirection.ToHome && s.EndedAt.HasValue).ToList();
        var lunchSessions = sessions.Where(s => s.State == TrackingState.Lunch && s.EndedAt.HasValue).ToList();

        var totalWorkMinutes = workSessions.Sum(s => (s.EndedAt!.Value - s.StartedAt).TotalMinutes);
        var totalCommuteToWorkMinutes = commuteToWorkSessions.Sum(s => (s.EndedAt!.Value - s.StartedAt).TotalMinutes);
        var totalCommuteToHomeMinutes = commuteToHomeSessions.Sum(s => (s.EndedAt!.Value - s.StartedAt).TotalMinutes);
        var totalCommuteMinutes = totalCommuteToWorkMinutes + totalCommuteToHomeMinutes;
        var totalLunchMinutes = lunchSessions.Sum(s => (s.EndedAt!.Value - s.StartedAt).TotalMinutes);

        // Office span: from first session start to last session end (completed sessions only)
        var completedSessions = sessions.Where(s => s.EndedAt.HasValue).ToList();
        double officeSpanMinutes = 0;
        if (completedSessions.Count > 0)
        {
            var firstStart = completedSessions.Min(s => s.StartedAt);
            var lastEnd = completedSessions.Max(s => s.EndedAt!.Value);
            officeSpanMinutes = (lastEnd - firstStart).TotalMinutes;
        }

        // Only show totals that have data
        if (workSessions.Count > 0)
        {
            var label = "Work total".PadRight(LabelWidth);
            var padding = new string(' ', TimeRangeWidth);
            builder.AppendLine($"{label} {padding}  {FormatMinutesAligned(totalWorkMinutes)}");
        }

        if (commuteToWorkSessions.Count > 0)
        {
            var label = "Commute to work".PadRight(LabelWidth);
            var padding = new string(' ', TimeRangeWidth);
            builder.AppendLine($"{label} {padding}  {FormatMinutesAligned(totalCommuteToWorkMinutes)}");
        }

        if (commuteToHomeSessions.Count > 0)
        {
            var label = "Commute to home".PadRight(LabelWidth);
            var padding = new string(' ', TimeRangeWidth);
            builder.AppendLine($"{label} {padding}  {FormatMinutesAligned(totalCommuteToHomeMinutes)}");
        }

        if (commuteToWorkSessions.Count > 0 || commuteToHomeSessions.Count > 0)
        {
            var label = "Commute total".PadRight(LabelWidth);
            var padding = new string(' ', TimeRangeWidth);
            builder.AppendLine($"{label} {padding}  {FormatMinutesAligned(totalCommuteMinutes)}");
        }

        if (lunchSessions.Count > 0)
        {
            var label = "Lunch total".PadRight(LabelWidth);
            var padding = new string(' ', TimeRangeWidth);
            builder.AppendLine($"{label} {padding}  {FormatMinutesAligned(totalLunchMinutes)}");
        }

        if (officeSpanMinutes > 0)
        {
            var label = "Office span".PadRight(LabelWidth);
            var padding = new string(' ', TimeRangeWidth);
            builder.AppendLine($"{label} {padding}  {FormatMinutesAligned(officeSpanMinutes)}");
        }

        builder.Append("</pre>");

        return builder.ToString();
    }

    /// <summary>
    /// Returns a human-readable label for a session.
    /// </summary>
    private static string FormatSessionLabel(TrackingSession session)
    {
        return session.State switch
        {
            TrackingState.Commuting => session.CommuteDirection == CommuteDirection.ToWork
                ? "Commute to work"
                : "Commute to home",
            TrackingState.Working => "Work",
            TrackingState.Lunch => "Lunch",
            _ => session.State.ToString()
        };
    }

    /// <summary>
    /// Formats a UTC timestamp for display, converted to user's local timezone.
    /// </summary>
    private static string FormatTime(DateTime utcTime, int utcOffsetMinutes)
    {
        var localTime = utcTime.AddMinutes(utcOffsetMinutes);
        return localTime.ToString("HH:mm");
    }

    /// <summary>
    /// Formats a duration between two timestamps, right-aligned in DurationWidth chars.
    /// Format: "Xh Ym" or "  Ym" (never decimal hours).
    /// </summary>
    private static string FormatDurationAligned(DateTime start, DateTime end)
    {
        return FormatMinutesAligned((end - start).TotalMinutes);
    }

    /// <summary>
    /// Formats a total number of minutes into "Xh Ym" or "Ym", right-aligned in DurationWidth.
    /// </summary>
    private static string FormatMinutesAligned(double totalMinutes)
    {
        var totalMins = (int)Math.Round(totalMinutes);
        var hours = totalMins / 60;
        var minutes = totalMins % 60;

        string raw;
        if (hours > 0 && minutes > 0)
            raw = $"{hours}h {minutes}m";
        else if (hours > 0)
            raw = $"{hours}h";
        else
            raw = $"{minutes}m";

        return raw.PadLeft(DurationWidth);
    }
}
