using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TimeSheet.Core.Application.Interfaces;
using TimeSheet.Core.Application.Models;
using TimeSheet.Core.Domain.Enums;

namespace TimeSheet.Presentation.Telegram.Handlers;

/// <summary>
/// Handles the /status command, which shows the current tracking state.
/// </summary>
public class StatusCommandHandler(
    ILogger<StatusCommandHandler> logger,
    IServiceScopeFactory serviceScopeFactory)
{
    // Callback data prefix for inline keyboard button
    private const string CallbackPrefix = "status:";
    private const string StopAction = "stop";

    /// <summary>
    /// Handles the /status command.
    /// </summary>
    public async Task HandleStatusAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        var userId = message.From?.Id;
        if (userId == null)
        {
            logger.LogWarning("Received /status message without user ID");
            return;
        }

        logger.LogInformation(
            "Status command from user {UserId} ({Username})",
            userId.Value,
            message.From?.Username ?? "Unknown");

        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var trackingSessionRepository = scope.ServiceProvider.GetRequiredService<ITrackingSessionRepository>();
            var timeTrackingService = scope.ServiceProvider.GetRequiredService<ITimeTrackingService>();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

            // Get the active session
            var activeSession = await trackingSessionRepository.GetActiveSessionAsync(
                userId.Value,
                cancellationToken);

            // Get user for timezone and target work hours
            var user = await userRepository.GetByTelegramUserIdAsync(userId.Value, cancellationToken);
            if (user == null)
            {
                logger.LogWarning("User {UserId} not found", userId.Value);
                return;
            }

            // Get today's sessions to calculate total work time
            var today = DateTime.UtcNow.Date;
            var todaysSessions = await timeTrackingService.GetTodaysSessionsAsync(
                userId.Value,
                today,
                cancellationToken);

            // Calculate total work time for today
            var totalWorkMinutes = todaysSessions
                .Where(s => s.State == TrackingState.Working && s.EndedAt.HasValue)
                .Sum(s => (s.EndedAt!.Value - s.StartedAt).TotalMinutes);

            // Add current work session if active
            if (activeSession?.State == TrackingState.Working)
            {
                totalWorkMinutes += (DateTime.UtcNow - activeSession.StartedAt).TotalMinutes;
            }

            string statusText;
            InlineKeyboardMarkup? keyboard = null;

            if (activeSession == null)
            {
                // No active session
                statusText = FormatIdleStatus(totalWorkMinutes, user.TargetWorkHours);
            }
            else
            {
                // Active session
                statusText = FormatActiveStatus(activeSession, totalWorkMinutes, user.TargetWorkHours, user.UtcOffsetMinutes);
                keyboard = CreateStopButton(activeSession.State);
            }

            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: statusText,
                parseMode: ParseMode.Markdown,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "Sent status to user {UserId}: {State}",
                userId.Value,
                activeSession?.State.ToString() ?? "Idle");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling /status command for user {UserId}", userId.Value);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "An error occurred while retrieving your status. Please try again.",
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Handles callback queries from the "Stop" button.
    /// </summary>
    public async Task HandleCallbackQueryAsync(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var userId = callbackQuery.From.Id;
        var data = callbackQuery.Data ?? string.Empty;

        if (!data.StartsWith(CallbackPrefix))
        {
            // Not a status callback
            return;
        }

        try
        {
            // Parse callback data: "status:stop:{state}"
            var parts = data.Split(':');
            if (parts.Length != 3 || parts[1] != StopAction)
            {
                logger.LogWarning("Invalid callback data format: {Data}", data);
                await botClient.AnswerCallbackQuery(
                    callbackQueryId: callbackQuery.Id,
                    text: "Invalid button data",
                    cancellationToken: cancellationToken);
                return;
            }

            var stateStr = parts[2];
            if (!Enum.TryParse<TrackingState>(stateStr, out var state))
            {
                logger.LogWarning("Invalid state in callback data: {State}", stateStr);
                await botClient.AnswerCallbackQuery(
                    callbackQueryId: callbackQuery.Id,
                    text: "Invalid state",
                    cancellationToken: cancellationToken);
                return;
            }

            // Stop the current tracking by transitioning to Idle
            using var scope = serviceScopeFactory.CreateScope();
            var timeTrackingService = scope.ServiceProvider.GetRequiredService<ITimeTrackingService>();

            var result = await timeTrackingService.StartStateAsync(
                userId,
                state,
                DateTime.UtcNow,
                cancellationToken);

            // Update the message based on the result
            if (callbackQuery.Message != null)
            {
                var stateDisplay = state switch
                {
                    TrackingState.Commuting => "commute",
                    TrackingState.Working => "work session",
                    TrackingState.Lunch => "lunch break",
                    _ => "tracking"
                };

                var messageText = result switch
                {
                    TrackingResult.SessionEnded => $"✓ Stopped {stateDisplay}",
                    TrackingResult.NoChange => "No active session to stop",
                    _ => "Status updated"
                };

                await botClient.EditMessageText(
                    chatId: callbackQuery.Message.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: messageText,
                    cancellationToken: cancellationToken);
            }

            await botClient.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: "✓ Tracking stopped",
                showAlert: false,
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "User {UserId} stopped {State} via status button: {ResultType}",
                userId,
                state,
                result.GetType().Name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling status callback query for user {UserId}", userId);
            await botClient.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: "An error occurred while stopping tracking",
                showAlert: true,
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Formats the status when no session is active.
    /// </summary>
    private static string FormatIdleStatus(double totalWorkMinutes, decimal? targetWorkHours)
    {
        var workHours = totalWorkMinutes / 60.0;
        var workDisplay = FormatDuration(TimeSpan.FromMinutes(totalWorkMinutes));

        var text = $"""
            📊 *Current Status*

            ⏱️ State: *Idle* (not tracking)
            📈 Work today: {workDisplay}
            """;

        if (targetWorkHours.HasValue)
        {
            var targetDisplay = FormatHours((double)targetWorkHours.Value);
            var percentage = targetWorkHours.Value > 0 ? (decimal)(workHours / (double)targetWorkHours.Value * 100) : 0;
            text += $"\n🎯 Target: {targetDisplay} ({percentage:F0}% complete)";
        }

        return text;
    }

    /// <summary>
    /// Formats the status when a session is active.
    /// </summary>
    private static string FormatActiveStatus(
        Core.Domain.Entities.TrackingSession session,
        double totalWorkMinutes,
        decimal? targetWorkHours,
        int utcOffsetMinutes)
    {
        var stateEmoji = session.State switch
        {
            TrackingState.Commuting => "🚗",
            TrackingState.Working => "💼",
            TrackingState.Lunch => "🍽️",
            _ => "⏱️"
        };

        var stateDisplay = session.State switch
        {
            TrackingState.Commuting => session.CommuteDirection == CommuteDirection.ToWork
                ? "Commuting to work"
                : "Commuting home",
            TrackingState.Working => "Working",
            TrackingState.Lunch => "Lunch break",
            _ => session.State.ToString()
        };

        var duration = DateTime.UtcNow - session.StartedAt;
        var durationDisplay = FormatDuration(duration);

        // Convert start time to user's local timezone
        var localStartTime = session.StartedAt.AddMinutes(utcOffsetMinutes);
        var startTimeDisplay = localStartTime.ToString("HH:mm");

        var text = $"""
            📊 *Current Status*

            {stateEmoji} State: *{stateDisplay}*
            ⏰ Started: {startTimeDisplay}
            ⏳ Duration: {durationDisplay}
            """;

        if (session.State == TrackingState.Working || totalWorkMinutes > 0)
        {
            var workDisplay = FormatDuration(TimeSpan.FromMinutes(totalWorkMinutes));
            text += $"\n📈 Work today: {workDisplay}";

            if (targetWorkHours.HasValue)
            {
                var workHours = totalWorkMinutes / 60.0;
                var targetDisplay = FormatHours((double)targetWorkHours.Value);
                var percentage = targetWorkHours.Value > 0 ? (decimal)(workHours / (double)targetWorkHours.Value * 100) : 0;
                text += $"\n🎯 Target: {targetDisplay} ({percentage:F0}% complete)";
            }
        }

        return text;
    }

    /// <summary>
    /// Formats a duration for display.
    /// </summary>
    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
        {
            var hours = (int)duration.TotalHours;
            var minutes = duration.Minutes;
            return minutes > 0 ? $"{hours}h {minutes}m" : $"{hours}h";
        }

        return $"{duration.Minutes}m";
    }

    /// <summary>
    /// Formats hours for display.
    /// </summary>
    private static string FormatHours(double hours)
    {
        if (hours == (int)hours)
        {
            return $"{(int)hours}h";
        }

        var h = (int)hours;
        var m = (int)((hours - h) * 60);
        return m > 0 ? $"{h}h {m}m" : $"{h}h";
    }

    /// <summary>
    /// Creates an inline keyboard with a stop button.
    /// </summary>
    private static InlineKeyboardMarkup CreateStopButton(TrackingState state)
    {
        var buttonText = state switch
        {
            TrackingState.Commuting => "Stop Commute",
            TrackingState.Working => "Stop Working",
            TrackingState.Lunch => "Stop Lunch",
            _ => "Stop"
        };

        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(buttonText, $"{CallbackPrefix}{StopAction}:{state}")
            }
        });
    }
}
