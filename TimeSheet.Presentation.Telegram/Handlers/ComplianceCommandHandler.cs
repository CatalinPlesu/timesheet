using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using TimeSheet.Core.Application.Interfaces.Services;
using TimeSheet.Core.Domain.Repositories;

namespace TimeSheet.Presentation.Telegram.Handlers;

/// <summary>
/// Handles the /compliance command, which shows compliance violations for a date range.
/// </summary>
public class ComplianceCommandHandler(
    ILogger<ComplianceCommandHandler> logger,
    IServiceScopeFactory serviceScopeFactory)
{
    /// <summary>
    /// Handles the /compliance command.
    /// Usage:
    ///   /compliance          — today's compliance status
    ///   /compliance week     — this week
    ///   /compliance month    — this month
    /// </summary>
    public async Task HandleComplianceAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken,
        string? expandedText = null)
    {
        var userId = message.From?.Id;
        if (userId == null)
        {
            logger.LogWarning("Received /compliance message without user ID");
            return;
        }

        try
        {
            var messageText = expandedText ?? message.Text ?? string.Empty;
            var parts = messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var period = parts.Length > 1 ? parts[1].ToLowerInvariant() : "week";

            using var scope = serviceScopeFactory.CreateScope();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var complianceEngine = scope.ServiceProvider.GetRequiredService<IComplianceRuleEngine>();

            var user = await userRepository.GetByTelegramUserIdAsync(userId.Value, cancellationToken);
            if (user == null)
            {
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "User not found.",
                    cancellationToken: cancellationToken);
                return;
            }

            var (from, to, periodLabel) = GetDateRange(period, user.UtcOffsetMinutes);

            var violations = await complianceEngine.EvaluateAsync(user.Id, from, to, cancellationToken);

            var responseText = FormatComplianceReport(violations, periodLabel, from, to);

            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: responseText,
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "User {UserId} viewed compliance report for {Period}: {ViolationCount} violations",
                userId.Value, period, violations.Count);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid period argument for /compliance command from user {UserId}", userId.Value);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Invalid period. Use: /compliance [today|week|month]\n\n" +
                      "Examples:\n" +
                      "  /compliance         — this week\n" +
                      "  /compliance today   — today only\n" +
                      "  /compliance week    — this week\n" +
                      "  /compliance month   — this month",
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling /compliance command for user {UserId}", userId.Value);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "An error occurred while checking compliance. Please try again.",
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Gets the date range for a given period string.
    /// </summary>
    private static (DateOnly from, DateOnly to, string label) GetDateRange(string period, int utcOffsetMinutes)
    {
        // Use user's local date (approximate via UTC offset)
        var localNow = DateTime.UtcNow.AddMinutes(utcOffsetMinutes);
        var today = DateOnly.FromDateTime(localNow);

        return period switch
        {
            "today" or "day" => (today, today, "Today"),
            "week" or "weekly" => GetWeekRange(today),
            "month" or "monthly" => GetMonthRange(today),
            _ => throw new ArgumentException($"Invalid period: {period}. Use today, week, or month.")
        };
    }

    private static (DateOnly from, DateOnly to, string label) GetWeekRange(DateOnly today)
    {
        var daysSinceMonday = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        var monday = today.AddDays(-daysSinceMonday);
        var sunday = monday.AddDays(6);
        return (monday, sunday, "This Week");
    }

    private static (DateOnly from, DateOnly to, string label) GetMonthRange(DateOnly today)
    {
        var firstDay = new DateOnly(today.Year, today.Month, 1);
        var lastDay = firstDay.AddMonths(1).AddDays(-1);
        return (firstDay, lastDay, $"{today:MMMM yyyy}");
    }

    /// <summary>
    /// Formats the compliance report as a Telegram message.
    /// </summary>
    private static string FormatComplianceReport(
        IReadOnlyList<Core.Application.Models.ComplianceViolation> violations,
        string periodLabel,
        DateOnly from,
        DateOnly to)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Compliance: {periodLabel}");
        builder.AppendLine($"Period: {from:yyyy-MM-dd} to {to:yyyy-MM-dd}");
        builder.AppendLine();

        if (violations.Count == 0)
        {
            builder.AppendLine("No compliance violations found.");
            builder.AppendLine("All tracked days meet the minimum requirements.");
            return builder.ToString();
        }

        builder.AppendLine($"Found {violations.Count} violation{(violations.Count == 1 ? "" : "s")}:");
        builder.AppendLine();

        foreach (var v in violations)
        {
            var actualStr = v.ActualHours.HasValue
                ? FormatHours((decimal)v.ActualHours.Value)
                : "unknown";
            var requiredStr = FormatHours((decimal)v.ThresholdHours);

            builder.AppendLine($"  {v.Date:yyyy-MM-dd} ({v.Date.DayOfWeek})");
            builder.AppendLine($"    Actual: {actualStr}  Required: {requiredStr}");
            builder.AppendLine($"    Rule: {v.RuleType}");
        }

        return builder.ToString();
    }

    /// <summary>
    /// Formats hours as "Xh Ym".
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
}
