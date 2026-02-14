using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using TimeSheet.Core.Application.Interfaces.Services;
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
    /// Formats the list of sessions for display.
    /// </summary>
    private static string FormatSessionsList(List<TrackingSession> sessions, DateTime date, int utcOffsetMinutes)
    {
        if (sessions.Count == 0)
        {
            return "No tracking entries for today.";
        }

        var builder = new StringBuilder();
        builder.AppendLine($"Today's entries ({date:yyyy-MM-dd}):");
        builder.AppendLine();

        for (int i = 0; i < sessions.Count; i++)
        {
            var session = sessions[i];
            var entryNumber = i + 1;

            // Format session type
            var typeDisplay = FormatSessionType(session);

            // Format times (converted to user's local timezone)
            var startTime = FormatTime(session.StartedAt, utcOffsetMinutes);
            var endTime = session.EndedAt.HasValue
                ? FormatTime(session.EndedAt.Value, utcOffsetMinutes)
                : "ongoing";

            // Format duration
            var duration = session.EndedAt.HasValue
                ? FormatDuration(session.StartedAt, session.EndedAt.Value)
                : "ongoing";

            builder.AppendLine($"{entryNumber}. {typeDisplay}");
            builder.AppendLine($"   Started: {startTime}");
            builder.AppendLine($"   Ended: {endTime}");
            builder.AppendLine($"   Duration: {duration}");

            if (i < sessions.Count - 1)
            {
                builder.AppendLine();
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Formats the session type for display.
    /// </summary>
    private static string FormatSessionType(TrackingSession session)
    {
        return session.State switch
        {
            TrackingState.Commuting => session.CommuteDirection == CommuteDirection.ToWork
                ? "Commute to work"
                : "Commute to home",
            TrackingState.Working => "Work session",
            TrackingState.Lunch => "Lunch break",
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
    /// Formats a duration between two timestamps.
    /// </summary>
    private static string FormatDuration(DateTime start, DateTime end)
    {
        var duration = end - start;

        if (duration.TotalHours >= 1)
        {
            var hours = (int)duration.TotalHours;
            var minutes = duration.Minutes;
            return minutes > 0 ? $"{hours}h {minutes}m" : $"{hours}h";
        }

        return $"{duration.Minutes}m";
    }
}
