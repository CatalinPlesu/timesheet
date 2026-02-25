using System.Globalization;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using TimeSheet.Core.Application.Interfaces.Services;
using TimeSheet.Core.Application.Interfaces.Persistence;
using TimeSheet.Core.Domain.Repositories;
using TimeSheet.Core.Application.Models;
using TimeSheet.Core.Domain.Enums;

namespace TimeSheet.Presentation.Telegram.Handlers;

/// <summary>
/// Handles the /report command to show period aggregates, plus standalone /week, /month, /stats commands.
/// </summary>
public class ReportCommandHandler(
    ILogger<ReportCommandHandler> logger,
    IServiceScopeFactory serviceScopeFactory,
    IChartGenerationService chartGenerationService)
{
    // ─── Public entry points ─────────────────────────────────────────────────

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
            var messageText = expandedText ?? message.Text ?? string.Empty;
            var parts = messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Default to day if no argument provided
            var period = parts.Length > 1 ? parts[1].ToLowerInvariant() : "day";

            using var scope = serviceScopeFactory.CreateScope();
            var reportingService = scope.ServiceProvider.GetRequiredService<IReportingService>();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var user = await userRepository.GetByTelegramUserIdAsync(userId.Value, cancellationToken);

            if (user == null)
            {
                await botClient.SendMessage(chatId: message.Chat.Id, text: "User not found.", cancellationToken: cancellationToken);
                return;
            }

            // Handle "all" subcommand: send every report as a separate message
            if (period == "all")
            {
                await SendAllReportsAsync(botClient, message, reportingService, userId.Value, user.UtcOffsetMinutes, cancellationToken);
                logger.LogInformation("User {UserId} viewed all reports", userId.Value);
                return;
            }

            string responseText;

            // Handle special report types
            if (period == "commute")
            {
                responseText = await GenerateCommuteReportAsync(reportingService, userId.Value, cancellationToken);
            }
            else if (period is "daily" or "stats")
            {
                responseText = await GenerateDailyAveragesReportAsync(reportingService, userId.Value, cancellationToken);
            }
            else if (period.StartsWith("chart"))
            {
                var chartParts = messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var chartType = chartParts.Length > 2 ? chartParts[2].ToLowerInvariant() : "breakdown";
                var chartPeriod = chartParts.Length > 3 ? chartParts[3].ToLowerInvariant() : "week";

                await SendChartReportAsync(botClient, message, reportingService, userId.Value, chartType, chartPeriod, cancellationToken);
                logger.LogInformation("User {UserId} viewed {ChartType} chart for {Period}", userId.Value, chartType, chartPeriod);
                return;
            }
            else if (period.StartsWith("table"))
            {
                var tableParts = period.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var tablePeriod = tableParts.Length > 1 ? tableParts[1] : "week";
                responseText = await GenerateTableReportAsync(reportingService, userId.Value, tablePeriod, user.UtcOffsetMinutes, cancellationToken);
            }
            else
            {
                // day / week / month / year
                var (startDate, endDate, periodLabel) = GetDateRange(period, user.UtcOffsetMinutes);

                if (period is "day" or "today")
                {
                    responseText = await GenerateDailySummaryAsync(reportingService, userId.Value, startDate, periodLabel, user.TargetWorkHours, user.TargetOfficeHours, user.UtcOffsetMinutes, cancellationToken);
                }
                else if (period is "week" or "weekly")
                {
                    responseText = await GenerateWeeklyReportAsync(reportingService, userId.Value, startDate, endDate, periodLabel, user.UtcOffsetMinutes, cancellationToken);
                }
                else if (period is "month" or "monthly")
                {
                    responseText = await GenerateMonthlyReportAsync(reportingService, userId.Value, startDate, endDate, periodLabel, user.UtcOffsetMinutes, cancellationToken);
                }
                else
                {
                    // year / yearly — original aggregate format
                    var aggregate = await reportingService.GetPeriodAggregateAsync(userId.Value, startDate, endDate, cancellationToken);
                    responseText = FormatPeriodReport(aggregate, periodLabel);
                }
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
                text: "Invalid period. Use: /report [day|week|month|year|commute|daily|stats|table|chart|all]\n\n" +
                      "Examples:\n" +
                      "  /report (or /r) — Today's summary\n" +
                      "  /week — This week breakdown\n" +
                      "  /month — This month breakdown\n" +
                      "  /stats — Daily averages (7/30/90 days)\n" +
                      "  /compliance — Compliance violations",
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
    /// Handles the /week command — weekly summary with day breakdown.
    /// </summary>
    public async Task HandleWeekAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        var userId = message.From?.Id;
        if (userId == null) return;

        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var reportingService = scope.ServiceProvider.GetRequiredService<IReportingService>();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var user = await userRepository.GetByTelegramUserIdAsync(userId.Value, cancellationToken);
            if (user == null) return;

            var (startDate, endDate, periodLabel) = GetDateRange("week", user.UtcOffsetMinutes);
            var text = await GenerateWeeklyReportAsync(reportingService, userId.Value, startDate, endDate, periodLabel, user.UtcOffsetMinutes, cancellationToken);

            await botClient.SendMessage(chatId: message.Chat.Id, text: text, cancellationToken: cancellationToken);
            logger.LogInformation("User {UserId} viewed /week report", userId.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling /week command for user {UserId}", userId.Value);
            await botClient.SendMessage(chatId: message.Chat.Id, text: "An error occurred. Please try again.", cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Handles the /month command — monthly summary with day breakdown.
    /// </summary>
    public async Task HandleMonthAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        var userId = message.From?.Id;
        if (userId == null) return;

        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var reportingService = scope.ServiceProvider.GetRequiredService<IReportingService>();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var user = await userRepository.GetByTelegramUserIdAsync(userId.Value, cancellationToken);
            if (user == null) return;

            var (startDate, endDate, periodLabel) = GetDateRange("month", user.UtcOffsetMinutes);
            var text = await GenerateMonthlyReportAsync(reportingService, userId.Value, startDate, endDate, periodLabel, user.UtcOffsetMinutes, cancellationToken);

            await botClient.SendMessage(chatId: message.Chat.Id, text: text, cancellationToken: cancellationToken);
            logger.LogInformation("User {UserId} viewed /month report", userId.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling /month command for user {UserId}", userId.Value);
            await botClient.SendMessage(chatId: message.Chat.Id, text: "An error occurred. Please try again.", cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Handles the /stats command — averages over 7/30/90 days.
    /// </summary>
    public async Task HandleStatsAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        var userId = message.From?.Id;
        if (userId == null) return;

        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var reportingService = scope.ServiceProvider.GetRequiredService<IReportingService>();

            var text = await GenerateDailyAveragesReportAsync(reportingService, userId.Value, cancellationToken);
            await botClient.SendMessage(chatId: message.Chat.Id, text: text, cancellationToken: cancellationToken);
            logger.LogInformation("User {UserId} viewed /stats", userId.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling /stats command for user {UserId}", userId.Value);
            await botClient.SendMessage(chatId: message.Chat.Id, text: "An error occurred. Please try again.", cancellationToken: cancellationToken);
        }
    }

    // ─── Private generation helpers ──────────────────────────────────────────

    /// <summary>
    /// Generates a rich daily summary for today including idle time and targets.
    /// </summary>
    private static async Task<string> GenerateDailySummaryAsync(
        IReportingService reportingService,
        long userId,
        DateTime startDate,
        string periodLabel,
        decimal? targetWorkHours,
        decimal? targetOfficeHours,
        int utcOffsetMinutes,
        CancellationToken cancellationToken)
    {
        var endDate = startDate.AddDays(1);
        var breakdown = await reportingService.GetDailyBreakdownAsync(userId, startDate, endDate, cancellationToken);

        var day = breakdown.FirstOrDefault();

        var builder = new StringBuilder();
        builder.AppendLine($"Report: {periodLabel}");
        builder.AppendLine();

        if (day == null || !day.HasActivity)
        {
            builder.AppendLine("No activity recorded today.");
            return builder.ToString();
        }

        // Core metrics
        builder.AppendLine($"Work:      {FormatHours(day.WorkHours)}");

        if (day.CommuteToWorkHours > 0)
            builder.AppendLine($"Commute->: {FormatHours(day.CommuteToWorkHours)}");
        if (day.CommuteToHomeHours > 0)
            builder.AppendLine($"Commute<-: {FormatHours(day.CommuteToHomeHours)}");

        if (day.LunchHours > 0)
            builder.AppendLine($"Lunch:     {FormatHours(day.LunchHours)}");

        // Office span and idle
        if (day.TotalDurationHours.HasValue)
        {
            var officeSpan = day.TotalDurationHours.Value;
            var idleHours = Math.Max(0m, officeSpan - day.WorkHours - day.LunchHours);
            builder.AppendLine($"Office:    {FormatHours(officeSpan)}");
            if (idleHours > 0.0833m) // only show if > 5 minutes
                builder.AppendLine($"Idle:      {FormatHours(idleHours)}");
        }

        // Target progress
        if (targetWorkHours.HasValue && targetWorkHours.Value > 0)
        {
            var pct = (int)Math.Round(day.WorkHours / targetWorkHours.Value * 100);
            builder.AppendLine();
            builder.AppendLine($"Work target: {FormatHours(targetWorkHours.Value)} ({pct}% done)");
        }

        if (targetOfficeHours.HasValue && targetOfficeHours.Value > 0 && day.TotalDurationHours.HasValue)
        {
            var pct = (int)Math.Round(day.TotalDurationHours.Value / targetOfficeHours.Value * 100);
            builder.AppendLine($"Office target: {FormatHours(targetOfficeHours.Value)} ({pct}% done)");
        }

        return builder.ToString();
    }

    /// <summary>
    /// Generates a weekly report with a per-day table and totals/averages.
    /// </summary>
    private static async Task<string> GenerateWeeklyReportAsync(
        IReportingService reportingService,
        long userId,
        DateTime startDate,
        DateTime endDate,
        string periodLabel,
        int utcOffsetMinutes,
        CancellationToken cancellationToken)
    {
        var breakdown = await reportingService.GetDailyBreakdownAsync(userId, startDate, endDate, cancellationToken);
        return FormatPeriodBreakdownReport(breakdown, periodLabel);
    }

    /// <summary>
    /// Generates a monthly report with a per-day table and totals/averages.
    /// </summary>
    private static async Task<string> GenerateMonthlyReportAsync(
        IReportingService reportingService,
        long userId,
        DateTime startDate,
        DateTime endDate,
        string periodLabel,
        int utcOffsetMinutes,
        CancellationToken cancellationToken)
    {
        var breakdown = await reportingService.GetDailyBreakdownAsync(userId, startDate, endDate, cancellationToken);
        return FormatPeriodBreakdownReport(breakdown, periodLabel);
    }

    /// <summary>
    /// Sends all report types as individual Telegram messages.
    /// </summary>
    private async Task SendAllReportsAsync(
        ITelegramBotClient botClient,
        Message message,
        IReportingService reportingService,
        long userId,
        int utcOffsetMinutes,
        CancellationToken cancellationToken)
    {
        // Day
        var (dayStart, dayEnd, dayLabel) = GetDateRange("day", utcOffsetMinutes);
        var dayAggregate = await reportingService.GetPeriodAggregateAsync(userId, dayStart, dayEnd, cancellationToken);
        await botClient.SendMessage(chatId: message.Chat.Id, text: FormatPeriodReport(dayAggregate, dayLabel), cancellationToken: cancellationToken);

        // Week
        var (weekStart, weekEnd, weekLabel) = GetDateRange("week", utcOffsetMinutes);
        var weekText = await GenerateWeeklyReportAsync(reportingService, userId, weekStart, weekEnd, weekLabel, utcOffsetMinutes, cancellationToken);
        await botClient.SendMessage(chatId: message.Chat.Id, text: weekText, cancellationToken: cancellationToken);

        // Month
        var (monthStart, monthEnd, monthLabel) = GetDateRange("month", utcOffsetMinutes);
        var monthText = await GenerateMonthlyReportAsync(reportingService, userId, monthStart, monthEnd, monthLabel, utcOffsetMinutes, cancellationToken);
        await botClient.SendMessage(chatId: message.Chat.Id, text: monthText, cancellationToken: cancellationToken);

        // Year
        var (yearStart, yearEnd, yearLabel) = GetDateRange("year", utcOffsetMinutes);
        var yearAggregate = await reportingService.GetPeriodAggregateAsync(userId, yearStart, yearEnd, cancellationToken);
        await botClient.SendMessage(chatId: message.Chat.Id, text: FormatPeriodReport(yearAggregate, yearLabel), cancellationToken: cancellationToken);

        // Commute
        var commuteText = await GenerateCommuteReportAsync(reportingService, userId, cancellationToken);
        await botClient.SendMessage(chatId: message.Chat.Id, text: commuteText, cancellationToken: cancellationToken);

        // Daily averages
        var dailyText = await GenerateDailyAveragesReportAsync(reportingService, userId, cancellationToken);
        await botClient.SendMessage(chatId: message.Chat.Id, text: dailyText, cancellationToken: cancellationToken);
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
        var toWorkPatterns = await reportingService.GetCommutePatternsAsync(userId, CommuteDirection.ToWork, cancellationToken);
        var toHomePatterns = await reportingService.GetCommutePatternsAsync(userId, CommuteDirection.ToHome, cancellationToken);
        return FormatCommutePatternsReport(toWorkPatterns, toHomePatterns);
    }

    // ─── Date range helpers ───────────────────────────────────────────────────

    /// <summary>
    /// Calculates the date range for the specified period, respecting the user's local timezone.
    /// </summary>
    private static (DateTime startDate, DateTime endDate, string periodLabel) GetDateRange(string period, int utcOffsetMinutes = 0)
    {
        // Use user's local date via UTC offset
        var localNow = DateTime.UtcNow.AddMinutes(utcOffsetMinutes);
        var today = localNow.Date;

        return period switch
        {
            "day" or "daily" or "today" => (today, today.AddDays(1), "Today"),
            "week" or "weekly" => GetWeekRange(today),
            "month" or "monthly" => GetMonthRange(today),
            "year" or "yearly" => GetYearRange(today),
            _ => throw new ArgumentException($"Invalid period: {period}. Use day, week, month, year, or commute.")
        };
    }

    private static (DateTime startDate, DateTime endDate, string periodLabel) GetWeekRange(DateTime today)
    {
        var daysSinceMonday = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        var monday = today.AddDays(-daysSinceMonday);
        var nextMonday = monday.AddDays(7);
        return (monday, nextMonday, "This Week");
    }

    private static (DateTime startDate, DateTime endDate, string periodLabel) GetMonthRange(DateTime today)
    {
        var startOfMonth = new DateTime(today.Year, today.Month, 1);
        var startOfNextMonth = startOfMonth.AddMonths(1);
        var monthName = today.ToString("MMMM yyyy", CultureInfo.InvariantCulture);
        return (startOfMonth, startOfNextMonth, monthName);
    }

    private static (DateTime startDate, DateTime endDate, string periodLabel) GetYearRange(DateTime today)
    {
        var startOfYear = new DateTime(today.Year, 1, 1);
        var startOfNextYear = new DateTime(today.Year + 1, 1, 1);
        return (startOfYear, startOfNextYear, today.Year.ToString());
    }

    // ─── Formatters ───────────────────────────────────────────────────────────

    /// <summary>
    /// Formats a period breakdown report (used for week/month) with a per-day table plus totals.
    /// </summary>
    private static string FormatPeriodBreakdownReport(List<DailyBreakdownRow> breakdown, string periodLabel)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Report: {periodLabel}");
        builder.AppendLine();

        var activeDays = breakdown.Where(d => d.HasActivity).ToList();

        if (activeDays.Count == 0)
        {
            builder.AppendLine("No activity recorded in this period.");
            return builder.ToString();
        }

        // Per-day table
        builder.AppendLine("```");
        builder.AppendLine("Date       Work  Comm  Lunch Idle  Office");
        builder.AppendLine("---------- ----- ----- ----- ----- ------");

        foreach (var day in activeDays)
        {
            var dateStr = day.Date.ToString("ddd dd/MM");
            var workStr = FormatHoursCompact(day.WorkHours);
            var commStr = FormatHoursCompact(day.CommuteToWorkHours + day.CommuteToHomeHours);
            var lunchStr = FormatHoursCompact(day.LunchHours);

            string idleStr;
            string officeStr;
            if (day.TotalDurationHours.HasValue)
            {
                var idle = Math.Max(0m, day.TotalDurationHours.Value - day.WorkHours - day.LunchHours);
                idleStr = idle > 0.0833m ? FormatHoursCompact(idle) : "  -  ";
                officeStr = FormatHoursCompact(day.TotalDurationHours.Value);
            }
            else
            {
                idleStr = "  -  ";
                officeStr = "  -   ";
            }

            builder.AppendLine($"{dateStr} {workStr,5} {commStr,5} {lunchStr,5} {idleStr,5} {officeStr,6}");
        }

        builder.AppendLine("```");
        builder.AppendLine();

        // Totals
        var totalWork = activeDays.Sum(d => d.WorkHours);
        var totalComm = activeDays.Sum(d => d.CommuteToWorkHours + d.CommuteToHomeHours);
        var totalLunch = activeDays.Sum(d => d.LunchHours);
        var totalIdle = activeDays
            .Where(d => d.TotalDurationHours.HasValue)
            .Sum(d => Math.Max(0m, d.TotalDurationHours!.Value - d.WorkHours - d.LunchHours));

        var n = activeDays.Count;
        builder.AppendLine($"Work days: {n}");
        builder.AppendLine($"Work:      {FormatHours(totalWork)}  (avg {FormatHours(totalWork / n)}/day)");

        if (totalComm > 0)
            builder.AppendLine($"Commute:   {FormatHours(totalComm)}  (avg {FormatHours(totalComm / n)}/day)");

        if (totalLunch > 0)
            builder.AppendLine($"Lunch:     {FormatHours(totalLunch)}  (avg {FormatHours(totalLunch / n)}/day)");

        if (totalIdle > 0)
            builder.AppendLine($"Idle:      {FormatHours(totalIdle)}  (avg {FormatHours(totalIdle / n)}/day)");

        return builder.ToString();
    }

    /// <summary>
    /// Formats the period aggregate report (used for year).
    /// </summary>
    private static string FormatPeriodReport(PeriodAggregate aggregate, string periodLabel)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Report: {periodLabel}");
        builder.AppendLine($"Period: {aggregate.StartDate:yyyy-MM-dd} to {aggregate.EndDate.AddDays(-1):yyyy-MM-dd}");
        builder.AppendLine();

        if (aggregate.WorkDaysCount == 0)
        {
            builder.AppendLine("No work sessions recorded in this period.");
        }
        else
        {
            builder.AppendLine($"Work days: {aggregate.WorkDaysCount}");
            builder.AppendLine($"Work:      {FormatHours(aggregate.TotalWorkHours)}");
            if (aggregate.TotalCommuteToWorkHours > 0)
                builder.AppendLine($"Commute->: {FormatHours(aggregate.TotalCommuteToWorkHours)}");
            if (aggregate.TotalCommuteToHomeHours > 0)
                builder.AppendLine($"Commute<-: {FormatHours(aggregate.TotalCommuteToHomeHours)}");
            if (aggregate.TotalCommuteHours > 0)
                builder.AppendLine($"Commute:   {FormatHours(aggregate.TotalCommuteHours)}");
            if (aggregate.TotalLunchHours > 0)
                builder.AppendLine($"Lunch:     {FormatHours(aggregate.TotalLunchHours)}");

            if (aggregate.TotalDurationHours.HasValue)
                builder.AppendLine($"Avg office span/day: {FormatHours(aggregate.TotalDurationHours.Value)}");

            builder.AppendLine();

            var avgWork = aggregate.TotalWorkHours / aggregate.WorkDaysCount;
            var avgCommute = aggregate.TotalCommuteHours / aggregate.WorkDaysCount;
            var avgLunch = aggregate.TotalLunchHours / aggregate.WorkDaysCount;

            builder.AppendLine("Daily averages:");
            builder.AppendLine($"  Work:    {FormatHours(avgWork)}");
            if (aggregate.TotalCommuteHours > 0)
                builder.AppendLine($"  Commute: {FormatHours(avgCommute)}");
            if (aggregate.TotalLunchHours > 0)
                builder.AppendLine($"  Lunch:   {FormatHours(avgLunch)}");
        }

        return builder.ToString();
    }

    /// <summary>
    /// Formats the daily averages report (7/30/90 days) — the /stats output.
    /// </summary>
    private static string FormatDailyAveragesReport(
        DailyAveragesReport report7Days,
        DailyAveragesReport report30Days,
        DailyAveragesReport report90Days)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Stats — Daily Averages");
        builder.AppendLine();

        AppendAveragesSection(builder, "Last 7 days", report7Days);
        AppendAveragesSection(builder, "Last 30 days", report30Days);
        AppendAveragesSection(builder, "Last 90 days", report90Days);

        return builder.ToString();
    }

    private static void AppendAveragesSection(StringBuilder builder, string label, DailyAveragesReport report)
    {
        builder.AppendLine($"{label} ({report.TotalWorkDays} work days):");

        if (report.TotalWorkDays == 0)
        {
            builder.AppendLine("  No data available");
            builder.AppendLine();
            return;
        }

        builder.AppendLine($"  Work:      {FormatHours(report.AverageWorkHours)}/day");

        if (report.AverageCommuteToWorkHours > 0)
            builder.AppendLine($"  Commute->: {FormatHours(report.AverageCommuteToWorkHours)}/day");
        if (report.AverageCommuteToHomeHours > 0)
            builder.AppendLine($"  Commute<-: {FormatHours(report.AverageCommuteToHomeHours)}/day");

        if (report.AverageLunchHours > 0)
            builder.AppendLine($"  Lunch:     {FormatHours(report.AverageLunchHours)}/day");

        if (report.AverageTotalDurationHours > 0)
        {
            var avgIdle = Math.Max(0m, report.AverageTotalDurationHours - report.AverageWorkHours - report.AverageLunchHours);
            builder.AppendLine($"  Office:    {FormatHours(report.AverageTotalDurationHours)}/day");
            if (avgIdle > 0.0833m)
                builder.AppendLine($"  Idle:      {FormatHours(avgIdle)}/day");
        }

        builder.AppendLine();
    }

    /// <summary>
    /// Formats the commute patterns report.
    /// </summary>
    private static string FormatCommutePatternsReport(
        List<CommutePattern> toWorkPatterns,
        List<CommutePattern> toHomePatterns)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Commute Patterns (last 90 days)");
        builder.AppendLine();

        if (toWorkPatterns.Count == 0 && toHomePatterns.Count == 0)
        {
            builder.AppendLine("No commute data available.");
            return builder.ToString();
        }

        var allDays = Enum.GetValues<DayOfWeek>()
            .OrderBy(d => d == DayOfWeek.Sunday ? 7 : (int)d)
            .ToList();

        var toWorkByDay = toWorkPatterns.ToDictionary(p => p.DayOfWeek);
        var toHomeByDay = toHomePatterns.ToDictionary(p => p.DayOfWeek);

        builder.AppendLine("```");
        builder.AppendLine("Day       ->Work Best   <-Home Best  ");
        builder.AppendLine("--------- ------ -----  ------ ------");

        foreach (var day in allDays)
        {
            var hasToWork = toWorkByDay.TryGetValue(day, out var toWorkPattern);
            var hasToHome = toHomeByDay.TryGetValue(day, out var toHomePattern);

            if (!hasToWork && !hasToHome) continue;

            var dayName = GetDayName(day).PadRight(9);

            var toWorkAvg = hasToWork && toWorkPattern != null
                ? FormatHoursCompactFixed(toWorkPattern.AverageDurationHours, 6)
                : "  -   ";
            var toWorkBest = hasToWork && toWorkPattern?.OptimalStartHour.HasValue == true
                ? $"{toWorkPattern.OptimalStartHour.Value:D2}:00"
                : "  -  ";

            var toHomeAvg = hasToHome && toHomePattern != null
                ? FormatHoursCompactFixed(toHomePattern.AverageDurationHours, 6)
                : "  -   ";
            var toHomeBest = hasToHome && toHomePattern?.OptimalStartHour.HasValue == true
                ? $"{toHomePattern.OptimalStartHour.Value:D2}:00"
                : "  -  ";

            builder.AppendLine($"{dayName} {toWorkAvg} {toWorkBest}  {toHomeAvg} {toHomeBest}");
        }

        builder.AppendLine("```");
        builder.AppendLine();
        builder.AppendLine("Best time = departure hour with shortest average commute (needs 2+ samples)");

        return builder.ToString();
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
        var (startDate, endDate, periodLabel) = GetDateRange(period, utcOffsetMinutes);

        var breakdown = await reportingService.GetDailyBreakdownAsync(
            userId,
            startDate,
            endDate,
            cancellationToken);

        return FormatTableReport(breakdown, periodLabel, utcOffsetMinutes);
    }

    /// <summary>
    /// Formats the daily breakdown report as a table (legacy table format).
    /// </summary>
    private static string FormatTableReport(
        List<DailyBreakdownRow> breakdown,
        string periodLabel,
        int utcOffsetMinutes)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Daily Breakdown: {periodLabel}");
        builder.AppendLine();

        if (breakdown.Count == 0)
        {
            builder.AppendLine("No data available for this period.");
            return builder.ToString();
        }

        var activeDays = breakdown.Where(d => d.HasActivity).ToList();

        if (activeDays.Count == 0)
        {
            builder.AppendLine("No work sessions recorded in this period.");
            return builder.ToString();
        }

        builder.AppendLine("```");
        builder.AppendLine("Date       Work  C->W  C->H  Lunch Idle  Total");
        builder.AppendLine("---------- ----- ----- ----- ----- ----- -----");

        foreach (var day in activeDays)
        {
            var dateStr = day.Date.ToString("yyyy-MM-dd");
            var workStr = FormatHoursCompact(day.WorkHours);
            var commuteToWorkStr = FormatHoursCompact(day.CommuteToWorkHours);
            var commuteToHomeStr = FormatHoursCompact(day.CommuteToHomeHours);
            var lunchStr = FormatHoursCompact(day.LunchHours);

            string idleStr;
            string totalStr;
            if (day.TotalDurationHours.HasValue)
            {
                var idle = Math.Max(0m, day.TotalDurationHours.Value - day.WorkHours - day.LunchHours);
                idleStr = idle > 0.0833m ? FormatHoursCompact(idle) : "  -  ";
                totalStr = FormatHoursCompact(day.TotalDurationHours.Value);
            }
            else
            {
                idleStr = "  -  ";
                totalStr = "  -  ";
            }

            builder.AppendLine($"{dateStr} {workStr,5} {commuteToWorkStr,5} {commuteToHomeStr,5} {lunchStr,5} {idleStr,5} {totalStr,5}");
        }

        builder.AppendLine("```");
        builder.AppendLine();

        var totalWork = activeDays.Sum(d => d.WorkHours);
        var totalCommuteToWork = activeDays.Sum(d => d.CommuteToWorkHours);
        var totalCommuteToHome = activeDays.Sum(d => d.CommuteToHomeHours);
        var totalLunch = activeDays.Sum(d => d.LunchHours);
        var totalIdle = activeDays
            .Where(d => d.TotalDurationHours.HasValue)
            .Sum(d => Math.Max(0m, d.TotalDurationHours!.Value - d.WorkHours - d.LunchHours));

        builder.AppendLine("Totals:");
        builder.AppendLine($"  Work: {FormatHours(totalWork)}");
        if (totalCommuteToWork > 0)
            builder.AppendLine($"  Commute to work: {FormatHours(totalCommuteToWork)}");
        if (totalCommuteToHome > 0)
            builder.AppendLine($"  Commute to home: {FormatHours(totalCommuteToHome)}");
        if (totalLunch > 0)
            builder.AppendLine($"  Lunch: {FormatHours(totalLunch)}");
        if (totalIdle > 0)
            builder.AppendLine($"  Idle: {FormatHours(totalIdle)}");
        builder.AppendLine();
        builder.AppendLine($"Work days: {activeDays.Count}");

        if (activeDays.Count > 0)
        {
            builder.AppendLine($"Avg work per day: {FormatHours(totalWork / activeDays.Count)}");
        }

        return builder.ToString();
    }

    // ─── Chart reports ────────────────────────────────────────────────────────

    /// <summary>
    /// Sends a chart report as an image to the user.
    /// </summary>
    private async Task SendChartReportAsync(
        ITelegramBotClient botClient,
        Message message,
        IReportingService reportingService,
        long userId,
        string chartType,
        string period,
        CancellationToken cancellationToken)
    {
        try
        {
            byte[] imageData;
            string caption;

            switch (chartType)
            {
                case "breakdown":
                    {
                        var (startDate, endDate, periodLabel) = GetDateRange(period);
                        var breakdown = await reportingService.GetDailyBreakdownAsync(userId, startDate, endDate, cancellationToken);
                        imageData = chartGenerationService.GenerateDailyBreakdownChart(breakdown, periodLabel);
                        caption = $"Daily Work Hours - {periodLabel}";
                        break;
                    }

                case "trend":
                    {
                        var (startDate, endDate, periodLabel) = GetDateRange(period);
                        var breakdown = await reportingService.GetDailyBreakdownAsync(userId, startDate, endDate, cancellationToken);
                        imageData = chartGenerationService.GenerateWorkHoursTrendChart(breakdown, periodLabel);
                        caption = $"Work Hours Trend - {periodLabel}";
                        break;
                    }

                case "activity":
                    {
                        var (startDate, endDate, periodLabel) = GetDateRange(period);
                        var breakdown = await reportingService.GetDailyBreakdownAsync(userId, startDate, endDate, cancellationToken);
                        imageData = chartGenerationService.GenerateActivityBreakdownChart(breakdown, periodLabel);
                        caption = $"Activity Breakdown - {periodLabel}";
                        break;
                    }

                case "averages":
                    {
                        var report7Days = await reportingService.GetDailyAveragesAsync(userId, 7, cancellationToken);
                        var report30Days = await reportingService.GetDailyAveragesAsync(userId, 30, cancellationToken);
                        var report90Days = await reportingService.GetDailyAveragesAsync(userId, 90, cancellationToken);
                        imageData = chartGenerationService.GenerateDailyAveragesComparisonChart(report7Days, report30Days, report90Days);
                        caption = "Daily Averages Comparison";
                        break;
                    }

                case "commute":
                    {
                        var toWorkPatterns = await reportingService.GetCommutePatternsAsync(userId, CommuteDirection.ToWork, cancellationToken);
                        var toHomePatterns = await reportingService.GetCommutePatternsAsync(userId, CommuteDirection.ToHome, cancellationToken);
                        imageData = chartGenerationService.GenerateCommutePatternsChart(toWorkPatterns, toHomePatterns);
                        caption = "Commute Patterns by Day of Week";
                        break;
                    }

                default:
                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: $"Unknown chart type: {chartType}\n\n" +
                              "Available chart types:\n" +
                              "  breakdown - Daily work hours bar chart\n" +
                              "  trend - Work hours trend line chart\n" +
                              "  activity - Stacked activity breakdown\n" +
                              "  averages - Daily averages comparison\n" +
                              "  commute - Commute patterns by day\n\n" +
                              "Example: /report chart breakdown week",
                        cancellationToken: cancellationToken);
                    return;
            }

            using var stream = new MemoryStream(imageData);
            var inputFile = InputFile.FromStream(stream, $"chart_{chartType}_{period}.png");

            await botClient.SendPhoto(
                chatId: message.Chat.Id,
                photo: inputFile,
                caption: caption,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating chart {ChartType} for user {UserId}", chartType, userId);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "An error occurred while generating the chart. Please try again.",
                cancellationToken: cancellationToken);
        }
    }

    // ─── Formatting utilities ─────────────────────────────────────────────────

    private static string GetDayName(DayOfWeek day) => day switch
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

    /// <summary>
    /// Formats hours as "Xh Ym" (e.g. "7h 30m").
    /// </summary>
    private static string FormatHours(decimal hours)
    {
        if (hours == 0) return "0h";
        var totalMinutes = (int)Math.Round(hours * 60);
        var h = totalMinutes / 60;
        var m = totalMinutes % 60;
        if (h > 0 && m > 0) return $"{h}h {m}m";
        if (h > 0) return $"{h}h";
        return $"{m}m";
    }

    /// <summary>
    /// Formats hours as a compact string for table display (e.g., "7h30m" or "30m").
    /// </summary>
    private static string FormatHoursCompact(decimal hours)
    {
        if (hours == 0) return "  -  ";
        var totalMinutes = (int)Math.Round(hours * 60);
        var h = totalMinutes / 60;
        var m = totalMinutes % 60;
        if (h > 0 && m > 0) return $"{h}h{m}m";
        if (h > 0) return $"{h}h";
        return $"{m}m";
    }

    /// <summary>
    /// Formats hours as a fixed-width compact string for table display.
    /// </summary>
    private static string FormatHoursCompactFixed(decimal hours, int width)
    {
        if (hours == 0) return new string(' ', width);
        var totalMinutes = (int)Math.Round(hours * 60);
        var h = totalMinutes / 60;
        var m = totalMinutes % 60;
        string result;
        if (h > 0 && m > 0) result = $"{h}h{m}m";
        else if (h > 0) result = $"{h}h";
        else result = $"{m}m";
        return result.PadLeft(width);
    }
}
