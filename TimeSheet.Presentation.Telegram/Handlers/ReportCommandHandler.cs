using System.Globalization;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using TimeSheet.Core.Application.Interfaces;
using TimeSheet.Core.Application.Models;
using TimeSheet.Core.Domain.Enums;

namespace TimeSheet.Presentation.Telegram.Handlers;

/// <summary>
/// Handles the /report command to show period aggregates.
/// </summary>
public class ReportCommandHandler(
    ILogger<ReportCommandHandler> logger,
    IServiceScopeFactory serviceScopeFactory)
{
    /// <summary>
    /// Handles the /report command.
    /// </summary>
    /// <param name="expandedText">Optional expanded alias text (e.g., "/report day" from "/r d")</param>
    public async Task HandleReportAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken,
        string? expandedText = null)
    {
        var userId = message.From?.Id;
        if (userId == null)
        {
            logger.LogWarning("Received /report message without user ID");
            return;
        }

        try
        {
            // Parse command arguments (use expanded text if provided, otherwise use message text)
            var messageText = expandedText ?? message.Text ?? string.Empty;
            var parts = messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Default to day if no argument provided
            var period = parts.Length > 1 ? parts[1].ToLowerInvariant() : "day";

            using var scope = serviceScopeFactory.CreateScope();
            var reportingService = scope.ServiceProvider.GetRequiredService<IReportingService>();

            // Handle "all" subcommand: send every report as a separate message
            if (period == "all")
            {
                await SendAllReportsAsync(botClient, message, reportingService, userId.Value, cancellationToken);
                logger.LogInformation("User {UserId} viewed all reports", userId.Value);
                return;
            }

            string responseText;

            // Handle special report types
            if (period == "commute")
            {
                responseText = await GenerateCommuteReportAsync(reportingService, userId.Value, cancellationToken);
            }
            else if (period == "daily")
            {
                responseText = await GenerateDailyAveragesReportAsync(reportingService, userId.Value, cancellationToken);
            }
            else if (period.StartsWith("table"))
            {
                // Table report: /report table [week|month|year]
                var tableParts = period.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var tablePeriod = tableParts.Length > 1 ? tableParts[1] : "week";

                var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                var user = await userRepository.GetByTelegramUserIdAsync(userId.Value, cancellationToken);
                if (user == null)
                {
                    responseText = "User not found.";
                }
                else
                {
                    responseText = await GenerateTableReportAsync(reportingService, userId.Value, tablePeriod, user.UtcOffsetMinutes, cancellationToken);
                }
            }
            else
            {
                // Calculate date range based on period
                var (startDate, endDate, periodLabel) = GetDateRange(period);

                // Generate the report
                var aggregate = await reportingService.GetPeriodAggregateAsync(
                    userId.Value,
                    startDate,
                    endDate,
                    cancellationToken);

                // Format the response
                responseText = FormatPeriodReport(aggregate, periodLabel);
            }

            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: responseText,
                cancellationToken: cancellationToken);

            logger.LogInformation("User {UserId} viewed {Period} report", userId.Value, period);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid period argument for /report command from user {UserId}", userId.Value);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Invalid period. Use: /report [day|week|month|year|commute|daily|table|all]\n\n" +
                      "Examples:\n" +
                      "  /report day - Today's summary\n" +
                      "  /report week - This week\n" +
                      "  /report month - This month\n" +
                      "  /report year - This year\n" +
                      "  /report commute - Commute patterns\n" +
                      "  /report daily - Daily averages (7/30/90 days)\n" +
                      "  /report table week - Daily breakdown table for week\n" +
                      "  /report table month - Daily breakdown table for month\n" +
                      "  /report all - All reports as separate messages",
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling /report command for user {UserId}", userId.Value);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "An error occurred while generating your report. Please try again.",
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Sends all report types as individual Telegram messages.
    /// </summary>
    private async Task SendAllReportsAsync(
        ITelegramBotClient botClient,
        Message message,
        IReportingService reportingService,
        long userId,
        CancellationToken cancellationToken)
    {
        // Day
        var (dayStart, dayEnd, dayLabel) = GetDateRange("day");
        var dayAggregate = await reportingService.GetPeriodAggregateAsync(userId, dayStart, dayEnd, cancellationToken);
        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: FormatPeriodReport(dayAggregate, dayLabel),
            cancellationToken: cancellationToken);

        // Week
        var (weekStart, weekEnd, weekLabel) = GetDateRange("week");
        var weekAggregate = await reportingService.GetPeriodAggregateAsync(userId, weekStart, weekEnd, cancellationToken);
        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: FormatPeriodReport(weekAggregate, weekLabel),
            cancellationToken: cancellationToken);

        // Month
        var (monthStart, monthEnd, monthLabel) = GetDateRange("month");
        var monthAggregate = await reportingService.GetPeriodAggregateAsync(userId, monthStart, monthEnd, cancellationToken);
        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: FormatPeriodReport(monthAggregate, monthLabel),
            cancellationToken: cancellationToken);

        // Year
        var (yearStart, yearEnd, yearLabel) = GetDateRange("year");
        var yearAggregate = await reportingService.GetPeriodAggregateAsync(userId, yearStart, yearEnd, cancellationToken);
        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: FormatPeriodReport(yearAggregate, yearLabel),
            cancellationToken: cancellationToken);

        // Commute
        var commuteText = await GenerateCommuteReportAsync(reportingService, userId, cancellationToken);
        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: commuteText,
            cancellationToken: cancellationToken);

        // Daily averages
        var dailyText = await GenerateDailyAveragesReportAsync(reportingService, userId, cancellationToken);
        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: dailyText,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Generates a daily averages report (7, 30, 90 days).
    /// </summary>
    private static async Task<string> GenerateDailyAveragesReportAsync(
        IReportingService reportingService,
        long userId,
        CancellationToken cancellationToken)
    {
        var report7Days = await reportingService.GetDailyAveragesAsync(userId, 7, cancellationToken);
        var report30Days = await reportingService.GetDailyAveragesAsync(userId, 30, cancellationToken);
        var report90Days = await reportingService.GetDailyAveragesAsync(userId, 90, cancellationToken);

        return FormatDailyAveragesReport(report7Days, report30Days, report90Days);
    }

    /// <summary>
    /// Generates a commute pattern report.
    /// </summary>
    private static async Task<string> GenerateCommuteReportAsync(
        IReportingService reportingService,
        long userId,
        CancellationToken cancellationToken)
    {
        // Generate patterns for both directions
        var toWorkPatterns = await reportingService.GetCommutePatternsAsync(
            userId,
            CommuteDirection.ToWork,
            cancellationToken);

        var toHomePatterns = await reportingService.GetCommutePatternsAsync(
            userId,
            CommuteDirection.ToHome,
            cancellationToken);

        // Format the response
        return FormatCommutePatternsReport(toWorkPatterns, toHomePatterns);
    }

    /// <summary>
    /// Calculates the date range for the specified period.
    /// </summary>
    private static (DateTime startDate, DateTime endDate, string periodLabel) GetDateRange(string period)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;

        return period switch
        {
            "day" or "daily" or "today" => (today, today.AddDays(1), "Today"),
            "week" or "weekly" => GetWeekRange(today),
            "month" or "monthly" => GetMonthRange(today),
            "year" or "yearly" => GetYearRange(today),
            _ => throw new ArgumentException($"Invalid period: {period}. Use day, week, month, year, or commute.")
        };
    }

    /// <summary>
    /// Gets the date range for the current week (Monday-Sunday).
    /// </summary>
    private static (DateTime startDate, DateTime endDate, string periodLabel) GetWeekRange(DateTime today)
    {
        // Find Monday of current week
        var daysSinceMonday = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        var monday = today.AddDays(-daysSinceMonday);
        var nextMonday = monday.AddDays(7);

        return (monday, nextMonday, "This Week");
    }

    /// <summary>
    /// Gets the date range for the current month.
    /// </summary>
    private static (DateTime startDate, DateTime endDate, string periodLabel) GetMonthRange(DateTime today)
    {
        var startOfMonth = new DateTime(today.Year, today.Month, 1);
        var startOfNextMonth = startOfMonth.AddMonths(1);

        var monthName = today.ToString("MMMM yyyy", CultureInfo.InvariantCulture);

        return (startOfMonth, startOfNextMonth, monthName);
    }

    /// <summary>
    /// Gets the date range for the current year.
    /// </summary>
    private static (DateTime startDate, DateTime endDate, string periodLabel) GetYearRange(DateTime today)
    {
        var startOfYear = new DateTime(today.Year, 1, 1);
        var startOfNextYear = new DateTime(today.Year + 1, 1, 1);

        return (startOfYear, startOfNextYear, today.Year.ToString());
    }

    /// <summary>
    /// Formats the period aggregate report for display.
    /// </summary>
    private static string FormatPeriodReport(PeriodAggregate aggregate, string periodLabel)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"📈 Report: {periodLabel}");
        builder.AppendLine($"Period: {aggregate.StartDate:yyyy-MM-dd} to {aggregate.EndDate.AddDays(-1):yyyy-MM-dd}");
        builder.AppendLine();

        if (aggregate.WorkDaysCount == 0)
        {
            builder.AppendLine("No work sessions recorded in this period.");
        }
        else
        {
            builder.AppendLine($"Work days: {aggregate.WorkDaysCount}");
            builder.AppendLine($"Total work hours: {FormatHours(aggregate.TotalWorkHours)}");
            builder.AppendLine($"Total commute time: {FormatHours(aggregate.TotalCommuteHours)}");
            builder.AppendLine($"Total lunch time: {FormatHours(aggregate.TotalLunchHours)}");

            // Display total duration from first to last activity
            if (aggregate.TotalDurationHours.HasValue)
            {
                builder.AppendLine($"Total duration (first to last): {FormatHours(aggregate.TotalDurationHours.Value)}");
            }

            builder.AppendLine();

            // Calculate averages
            var avgWorkPerDay = aggregate.TotalWorkHours / aggregate.WorkDaysCount;
            var avgCommutePerDay = aggregate.TotalCommuteHours / aggregate.WorkDaysCount;
            var avgLunchPerDay = aggregate.TotalLunchHours / aggregate.WorkDaysCount;

            builder.AppendLine("Daily averages:");
            builder.AppendLine($"  Work: {FormatHours(avgWorkPerDay)}");
            builder.AppendLine($"  Commute: {FormatHours(avgCommutePerDay)}");
            builder.AppendLine($"  Lunch: {FormatHours(avgLunchPerDay)}");
        }

        return builder.ToString();
    }

    /// <summary>
    /// Formats the daily averages report for display.
    /// </summary>
    private static string FormatDailyAveragesReport(
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
            builder.AppendLine($"  Avg total duration: {FormatHours(report7Days.AverageTotalDurationHours)}");
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
            builder.AppendLine($"  Avg total duration: {FormatHours(report30Days.AverageTotalDurationHours)}");
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
            builder.AppendLine($"  Avg total duration: {FormatHours(report90Days.AverageTotalDurationHours)}");
        }
        else
        {
            builder.AppendLine("  No data available");
        }

        return builder.ToString();
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
    /// Generates a daily breakdown table report for a specific period.
    /// </summary>
    private static async Task<string> GenerateTableReportAsync(
        IReportingService reportingService,
        long userId,
        string period,
        int utcOffsetMinutes,
        CancellationToken cancellationToken)
    {
        var (startDate, endDate, periodLabel) = GetDateRange(period);

        var breakdown = await reportingService.GetDailyBreakdownAsync(
            userId,
            startDate,
            endDate,
            cancellationToken);

        return FormatTableReport(breakdown, periodLabel, utcOffsetMinutes);
    }

    /// <summary>
    /// Formats the daily breakdown report as a table.
    /// </summary>
    private static string FormatTableReport(
        List<DailyBreakdownRow> breakdown,
        string periodLabel,
        int utcOffsetMinutes)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"📊 Daily Breakdown: {periodLabel}");
        builder.AppendLine();

        if (breakdown.Count == 0)
        {
            builder.AppendLine("No data available for this period.");
            return builder.ToString();
        }

        // Filter out days with no activity for cleaner display
        var activeDays = breakdown.Where(d => d.HasActivity).ToList();

        if (activeDays.Count == 0)
        {
            builder.AppendLine("No work sessions recorded in this period.");
            return builder.ToString();
        }

        // Use monospace formatting for table alignment
        builder.AppendLine("```");

        // Header
        builder.AppendLine("Date       Work  C->W  C->H  Lunch Total");
        builder.AppendLine("---------- ----- ----- ----- ----- -----");

        // Data rows
        foreach (var day in activeDays)
        {
            var dateStr = day.Date.ToString("yyyy-MM-dd");
            var workStr = FormatHoursCompact(day.WorkHours);
            var commuteToWorkStr = FormatHoursCompact(day.CommuteToWorkHours);
            var commuteToHomeStr = FormatHoursCompact(day.CommuteToHomeHours);
            var lunchStr = FormatHoursCompact(day.LunchHours);
            var totalStr = day.TotalDurationHours.HasValue
                ? FormatHoursCompact(day.TotalDurationHours.Value)
                : "  -  ";

            builder.AppendLine($"{dateStr} {workStr,5} {commuteToWorkStr,5} {commuteToHomeStr,5} {lunchStr,5} {totalStr,5}");
        }

        builder.AppendLine("```");
        builder.AppendLine();

        // Summary totals
        var totalWork = activeDays.Sum(d => d.WorkHours);
        var totalCommuteToWork = activeDays.Sum(d => d.CommuteToWorkHours);
        var totalCommuteToHome = activeDays.Sum(d => d.CommuteToHomeHours);
        var totalLunch = activeDays.Sum(d => d.LunchHours);

        builder.AppendLine("Totals:");
        builder.AppendLine($"  Work: {FormatHours(totalWork)}");
        builder.AppendLine($"  Commute to work: {FormatHours(totalCommuteToWork)}");
        builder.AppendLine($"  Commute to home: {FormatHours(totalCommuteToHome)}");
        builder.AppendLine($"  Lunch: {FormatHours(totalLunch)}");
        builder.AppendLine();
        builder.AppendLine($"Work days: {activeDays.Count}");

        if (activeDays.Count > 0)
        {
            var avgWork = totalWork / activeDays.Count;
            builder.AppendLine($"Avg work per day: {FormatHours(avgWork)}");
        }

        return builder.ToString();
    }

    /// <summary>
    /// Formats hours as a compact string for table display (e.g., "7.5h" or "30m").
    /// </summary>
    private static string FormatHoursCompact(decimal hours)
    {
        if (hours == 0)
        {
            return "  -  ";
        }

        var totalMinutes = (int)Math.Round(hours * 60);
        var h = totalMinutes / 60;
        var m = totalMinutes % 60;

        if (h > 0 && m > 0)
        {
            // Format as "7.5h" style
            return $"{hours:0.0}h";
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
