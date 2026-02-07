using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using TimeSheet.Core.Application.Interfaces;
using TimeSheet.Core.Application.Models;
using TimeSheet.Core.Application.Services;

namespace TimeSheet.Presentation.Telegram.Handlers;

/// <summary>
/// Handles the /dailystats command to show average daily statistics.
/// </summary>
public class DailyStatsCommandHandler(
    ILogger<DailyStatsCommandHandler> logger,
    IServiceScopeFactory serviceScopeFactory)
{
    /// <summary>
    /// Handles the /dailystats command.
    /// </summary>
    public async Task HandleDailyStatsAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        var userId = message.From?.Id;
        if (userId == null)
        {
            logger.LogWarning("Received /dailystats message without user ID");
            return;
        }

        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var reportingService = scope.ServiceProvider.GetRequiredService<ReportingService>();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

            // Get user for timezone info
            var user = await userRepository.GetByTelegramUserIdAsync(userId.Value, cancellationToken);
            if (user == null)
            {
                logger.LogWarning("User {UserId} not found in database", userId.Value);
                return;
            }

            // Generate reports for 7, 30, and 90 days
            var report7Days = await reportingService.GetDailyAveragesAsync(userId.Value, 7, cancellationToken);
            var report30Days = await reportingService.GetDailyAveragesAsync(userId.Value, 30, cancellationToken);
            var report90Days = await reportingService.GetDailyAveragesAsync(userId.Value, 90, cancellationToken);

            // Format the response
            var responseText = FormatDailyStatsReport(report7Days, report30Days, report90Days);

            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: responseText,
                cancellationToken: cancellationToken);

            logger.LogInformation("User {UserId} viewed daily stats", userId.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling /dailystats command for user {UserId}", userId.Value);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "An error occurred while generating your daily statistics. Please try again.",
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Formats the daily stats report for display.
    /// </summary>
    private static string FormatDailyStatsReport(
        DailyAveragesReport report7Days,
        DailyAveragesReport report30Days,
        DailyAveragesReport report90Days)
    {
        var builder = new StringBuilder();
        builder.AppendLine("📊 Daily Averages Report");
        builder.AppendLine();

        // 7 days
        builder.AppendLine("Last 7 days:");
        if (report7Days.TotalWorkDays > 0)
        {
            builder.AppendLine($"  Work days: {report7Days.TotalWorkDays}");
            builder.AppendLine($"  Avg work: {FormatHours(report7Days.AverageWorkHours)}");
            builder.AppendLine($"  Avg commute to work: {FormatHours(report7Days.AverageCommuteToWorkHours)}");
            builder.AppendLine($"  Avg commute to home: {FormatHours(report7Days.AverageCommuteToHomeHours)}");
            builder.AppendLine($"  Avg lunch: {FormatHours(report7Days.AverageLunchHours)}");
        }
        else
        {
            builder.AppendLine("  No data available");
        }
        builder.AppendLine();

        // 30 days
        builder.AppendLine("Last 30 days:");
        if (report30Days.TotalWorkDays > 0)
        {
            builder.AppendLine($"  Work days: {report30Days.TotalWorkDays}");
            builder.AppendLine($"  Avg work: {FormatHours(report30Days.AverageWorkHours)}");
            builder.AppendLine($"  Avg commute to work: {FormatHours(report30Days.AverageCommuteToWorkHours)}");
            builder.AppendLine($"  Avg commute to home: {FormatHours(report30Days.AverageCommuteToHomeHours)}");
            builder.AppendLine($"  Avg lunch: {FormatHours(report30Days.AverageLunchHours)}");
        }
        else
        {
            builder.AppendLine("  No data available");
        }
        builder.AppendLine();

        // 90 days
        builder.AppendLine("Last 90 days:");
        if (report90Days.TotalWorkDays > 0)
        {
            builder.AppendLine($"  Work days: {report90Days.TotalWorkDays}");
            builder.AppendLine($"  Avg work: {FormatHours(report90Days.AverageWorkHours)}");
            builder.AppendLine($"  Avg commute to work: {FormatHours(report90Days.AverageCommuteToWorkHours)}");
            builder.AppendLine($"  Avg commute to home: {FormatHours(report90Days.AverageCommuteToHomeHours)}");
            builder.AppendLine($"  Avg lunch: {FormatHours(report90Days.AverageLunchHours)}");
        }
        else
        {
            builder.AppendLine("  No data available");
        }

        return builder.ToString();
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
