using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using TimeSheet.Core.Application.Interfaces;
using TimeSheet.Core.Application.Models;
using TimeSheet.Core.Domain.Enums;

namespace TimeSheet.Presentation.Telegram.Handlers;

/// <summary>
/// Handles the /commutepatterns command to show commute pattern analysis.
/// </summary>
public class CommutePatternsCommandHandler(
    ILogger<CommutePatternsCommandHandler> logger,
    IServiceScopeFactory serviceScopeFactory)
{
    /// <summary>
    /// Handles the /commutepatterns command.
    /// </summary>
    public async Task HandleCommutePatternsAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        var userId = message.From?.Id;
        if (userId == null)
        {
            logger.LogWarning("Received /commutepatterns message without user ID");
            return;
        }

        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var reportingService = scope.ServiceProvider.GetRequiredService<IReportingService>();

            // Generate patterns for both directions
            var toWorkPatterns = await reportingService.GetCommutePatternsAsync(
                userId.Value,
                CommuteDirection.ToWork,
                cancellationToken);

            var toHomePatterns = await reportingService.GetCommutePatternsAsync(
                userId.Value,
                CommuteDirection.ToHome,
                cancellationToken);

            // Format the response
            var responseText = FormatCommutePatternsReport(toWorkPatterns, toHomePatterns);

            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: responseText,
                cancellationToken: cancellationToken);

            logger.LogInformation("User {UserId} viewed commute patterns", userId.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling /commutepatterns command for user {UserId}", userId.Value);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "An error occurred while analyzing your commute patterns. Please try again.",
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Formats the commute patterns report for display.
    /// </summary>
    private static string FormatCommutePatternsReport(
        List<CommutePattern> toWorkPatterns,
        List<CommutePattern> toHomePatterns)
    {
        var builder = new StringBuilder();
        builder.AppendLine("🚗 Commute Pattern Analysis");
        builder.AppendLine("(Based on last 90 days)");
        builder.AppendLine();

        // To Work patterns
        builder.AppendLine("📍 Commute to Work:");
        if (toWorkPatterns.Count > 0)
        {
            foreach (var pattern in toWorkPatterns)
            {
                builder.AppendLine($"\n{GetDayName(pattern.DayOfWeek)}:");
                builder.AppendLine($"  Sessions: {pattern.SessionCount}");
                builder.AppendLine($"  Avg duration: {FormatHours(pattern.AverageDurationHours)}");

                if (pattern.OptimalStartHour.HasValue && pattern.ShortestDurationHours.HasValue)
                {
                    builder.AppendLine($"  Best time to leave: {pattern.OptimalStartHour.Value:D2}:00");
                    builder.AppendLine($"  Shortest commute: {FormatHours(pattern.ShortestDurationHours.Value)}");
                }
            }
        }
        else
        {
            builder.AppendLine("  No data available");
        }

        builder.AppendLine();

        // To Home patterns
        builder.AppendLine("🏠 Commute to Home:");
        if (toHomePatterns.Count > 0)
        {
            foreach (var pattern in toHomePatterns)
            {
                builder.AppendLine($"\n{GetDayName(pattern.DayOfWeek)}:");
                builder.AppendLine($"  Sessions: {pattern.SessionCount}");
                builder.AppendLine($"  Avg duration: {FormatHours(pattern.AverageDurationHours)}");

                if (pattern.OptimalStartHour.HasValue && pattern.ShortestDurationHours.HasValue)
                {
                    builder.AppendLine($"  Best time to leave: {pattern.OptimalStartHour.Value:D2}:00");
                    builder.AppendLine($"  Shortest commute: {FormatHours(pattern.ShortestDurationHours.Value)}");
                }
            }
        }
        else
        {
            builder.AppendLine("  No data available");
        }

        return builder.ToString();
    }

    /// <summary>
    /// Gets the display name for a day of the week.
    /// </summary>
    private static string GetDayName(DayOfWeek day)
    {
        return day switch
        {
            DayOfWeek.Monday => "Monday",
            DayOfWeek.Tuesday => "Tuesday",
            DayOfWeek.Wednesday => "Wednesday",
            DayOfWeek.Thursday => "Thursday",
            DayOfWeek.Friday => "Friday",
            DayOfWeek.Saturday => "Saturday",
            DayOfWeek.Sunday => "Sunday",
            _ => day.ToString()
        };
    }

    /// <summary>
    /// Formats hours as a human-readable string.
    /// </summary>
    private static string FormatHours(decimal hours)
    {
        if (hours == 0)
        {
            return "0h";
        }

        var totalMinutes = (int)Math.Round(hours * 60);
        var h = totalMinutes / 60;
        var m = totalMinutes % 60;

        if (h > 0 && m > 0)
        {
            return $"{h}h {m}m";
        }
        else if (h > 0)
        {
            return $"{h}h";
        }
        else
        {
            return $"{m}m";
        }
    }
}
