using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TimeSheet.Core.Application.Interfaces;
using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.Enums;
using TimeSheet.Infrastructure.Persistence;

namespace TimeSheet.Presentation.Telegram.Handlers;

/// <summary>
/// Handles the /edit command for editing tracking session timestamps.
/// Supports editing the most recent entry or selecting by entry ID from /list.
/// </summary>
public class EditCommandHandler(
    ILogger<EditCommandHandler> logger,
    IServiceScopeFactory serviceScopeFactory)
{
    // Callback data prefixes for inline keyboard buttons
    private const string CallbackPrefix = "edit:";

    /// <summary>
    /// Handles the /edit command.
    /// </summary>
    /// <param name="botClient">The Telegram bot client.</param>
    /// <param name="message">The incoming message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task HandleEditAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        var userId = message.From?.Id;
        if (userId == null)
        {
            logger.LogWarning("Received /edit message without user ID");
            return;
        }

        var messageText = message.Text ?? string.Empty;
        var parts = messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Parse the entry ID parameter (optional)
        int? entryId = null;
        if (parts.Length > 1)
        {
            if (int.TryParse(parts[1], out var id) && id > 0)
            {
                entryId = id;
            }
            else
            {
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "Invalid entry ID. Please provide a positive number from the /list command.",
                    cancellationToken: cancellationToken);
                return;
            }
        }

        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var trackingSessionRepository = scope.ServiceProvider.GetRequiredService<ITrackingSessionRepository>();
            var timeTrackingService = scope.ServiceProvider.GetRequiredService<ITimeTrackingService>();

            TrackingSession sessionToEdit;
            string displayLabel;

            if (entryId.HasValue)
            {
                // Get today's sessions for ID-based lookup
                var today = DateTime.UtcNow.Date;
                var todaysSessions = await timeTrackingService.GetTodaysSessionsAsync(
                    userId.Value,
                    today,
                    cancellationToken);

                if (todaysSessions.Count == 0)
                {
                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: "You don't have any tracking entries for today.",
                        cancellationToken: cancellationToken);
                    return;
                }

                // Check if entry ID is valid (1-based index)
                var entryIndex = entryId.Value - 1;
                if (entryIndex < 0 || entryIndex >= todaysSessions.Count)
                {
                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: $"Entry ID {entryId.Value} not found. You have {todaysSessions.Count} entries today. Use /list to see all entries.",
                        cancellationToken: cancellationToken);
                    return;
                }

                sessionToEdit = todaysSessions[entryIndex];
                displayLabel = $"Entry #{entryId.Value}";
            }
            else
            {
                // No ID provided - edit most recent entry
                var recentSessions = await trackingSessionRepository.GetRecentSessionsAsync(
                    userId.Value,
                    1,
                    cancellationToken);

                if (recentSessions.Count == 0)
                {
                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: "You don't have any tracking entries to edit.",
                        cancellationToken: cancellationToken);
                    return;
                }

                sessionToEdit = recentSessions[0];
                displayLabel = "Most recent entry";
            }

            // Format the session details
            var sessionDetails = FormatSessionDetails(sessionToEdit, displayLabel);

            // Create inline keyboard with adjustment buttons
            var keyboard = CreateAdjustmentKeyboard(sessionToEdit.Id);

            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: sessionDetails,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "User {UserId} opened edit interface for session {SessionId} ({Label})",
                userId.Value,
                sessionToEdit.Id,
                displayLabel);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling /edit command for user {UserId}", userId.Value);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "An error occurred while retrieving your tracking entries. Please try again.",
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Handles callback queries from inline keyboard button presses.
    /// </summary>
    /// <param name="botClient">The Telegram bot client.</param>
    /// <param name="callbackQuery">The callback query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task HandleCallbackQueryAsync(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var userId = callbackQuery.From.Id;
        var data = callbackQuery.Data ?? string.Empty;

        if (!data.StartsWith(CallbackPrefix))
        {
            // Not an edit callback
            return;
        }

        try
        {
            // Parse callback data: "edit:{sessionId}:{adjustment}"
            // Example: "edit:abc123:+5" means add 5 minutes to session abc123
            var parts = data.Split(':');
            if (parts.Length != 3)
            {
                logger.LogWarning("Invalid callback data format: {Data}", data);
                await botClient.AnswerCallbackQuery(
                    callbackQueryId: callbackQuery.Id,
                    text: "Invalid button data",
                    cancellationToken: cancellationToken);
                return;
            }

            var sessionIdStr = parts[1];
            var adjustmentStr = parts[2];

            if (!Guid.TryParse(sessionIdStr, out var sessionId))
            {
                logger.LogWarning("Invalid session ID in callback data: {SessionId}", sessionIdStr);
                await botClient.AnswerCallbackQuery(
                    callbackQueryId: callbackQuery.Id,
                    text: "Invalid session ID",
                    cancellationToken: cancellationToken);
                return;
            }

            if (!int.TryParse(adjustmentStr, out var adjustmentMinutes))
            {
                logger.LogWarning("Invalid adjustment value in callback data: {Adjustment}", adjustmentStr);
                await botClient.AnswerCallbackQuery(
                    callbackQueryId: callbackQuery.Id,
                    text: "Invalid adjustment value",
                    cancellationToken: cancellationToken);
                return;
            }

            using var scope = serviceScopeFactory.CreateScope();
            var trackingSessionRepository = scope.ServiceProvider.GetRequiredService<ITrackingSessionRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Get the session
            var session = await trackingSessionRepository.GetByIdAsync(sessionId, cancellationToken);
            if (session == null)
            {
                await botClient.AnswerCallbackQuery(
                    callbackQueryId: callbackQuery.Id,
                    text: "Session not found",
                    cancellationToken: cancellationToken);
                return;
            }

            // Verify ownership
            if (session.UserId != userId)
            {
                logger.LogWarning(
                    "User {UserId} attempted to edit session {SessionId} owned by {OwnerId}",
                    userId,
                    sessionId,
                    session.UserId);
                await botClient.AnswerCallbackQuery(
                    callbackQueryId: callbackQuery.Id,
                    text: "You can only edit your own sessions",
                    cancellationToken: cancellationToken);
                return;
            }

            // Apply the adjustment using reflection (TrackingSession properties are init-only)
            // We need to create a new entity with adjusted timestamps
            var newStartedAt = session.StartedAt.AddMinutes(adjustmentMinutes);
            var newEndedAt = session.EndedAt?.AddMinutes(adjustmentMinutes);

            // Validate the adjustment
            if (newEndedAt.HasValue && newEndedAt.Value < newStartedAt)
            {
                await botClient.AnswerCallbackQuery(
                    callbackQueryId: callbackQuery.Id,
                    text: "Adjustment would create invalid duration (end before start)",
                    showAlert: true,
                    cancellationToken: cancellationToken);
                return;
            }

            // Detach the current entity to avoid tracking conflicts
            dbContext.Entry(session).State = EntityState.Detached;

            // Create a new session with adjusted timestamps
            var adjustedSession = new TrackingSession(
                id: session.Id,
                userId: session.UserId,
                state: session.State,
                startedAt: newStartedAt,
                endedAt: newEndedAt,
                commuteDirection: session.CommuteDirection);

            // Update the session
            trackingSessionRepository.Update(adjustedSession);
            await unitOfWork.CompleteAsync(cancellationToken);

            // Update the message with new details
            var updatedDetails = FormatSessionDetails(adjustedSession, "Editing entry");
            var keyboard = CreateAdjustmentKeyboard(sessionId);

            if (callbackQuery.Message != null)
            {
                await botClient.EditMessageText(
                    chatId: callbackQuery.Message.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: updatedDetails,
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken);
            }

            // Answer the callback query with visual feedback
            var feedbackText = adjustmentMinutes > 0
                ? $"⏰ Adjusted by +{adjustmentMinutes} minutes"
                : $"⏰ Adjusted by {adjustmentMinutes} minutes";

            await botClient.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: feedbackText,
                showAlert: false, // Small popup instead of full alert
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "User {UserId} adjusted session {SessionId} by {Minutes} minutes",
                userId,
                sessionId,
                adjustmentMinutes);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling edit callback query for user {UserId}", userId);
            await botClient.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: "An error occurred while updating the session",
                showAlert: true,
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Formats the session details for display.
    /// </summary>
    private static string FormatSessionDetails(TrackingSession session, string displayLabel)
    {
        var stateDisplay = session.State switch
        {
            TrackingState.Commuting => session.CommuteDirection == CommuteDirection.ToWork
                ? "Commute to work"
                : "Commute to home",
            TrackingState.Working => "Work session",
            TrackingState.Lunch => "Lunch break",
            _ => session.State.ToString()
        };

        var startTime = session.StartedAt.ToString("yyyy-MM-dd HH:mm");
        var endTime = session.EndedAt?.ToString("HH:mm") ?? "ongoing";

        var duration = session.EndedAt.HasValue
            ? FormatDuration(session.StartedAt, session.EndedAt.Value)
            : "ongoing";

        return $"{displayLabel}:\n" +
               $"Type: {stateDisplay}\n" +
               $"Started: {startTime}\n" +
               $"Ended: {endTime}\n" +
               $"Duration: {duration}\n\n" +
               $"Select an adjustment:";
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

    /// <summary>
    /// Creates an inline keyboard with time adjustment buttons.
    /// </summary>
    private static InlineKeyboardMarkup CreateAdjustmentKeyboard(Guid sessionId)
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("-30m", $"{CallbackPrefix}{sessionId}:-30"),
                InlineKeyboardButton.WithCallbackData("-5m", $"{CallbackPrefix}{sessionId}:-5"),
                InlineKeyboardButton.WithCallbackData("-1m", $"{CallbackPrefix}{sessionId}:-1"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("+1m", $"{CallbackPrefix}{sessionId}:+1"),
                InlineKeyboardButton.WithCallbackData("+5m", $"{CallbackPrefix}{sessionId}:+5"),
                InlineKeyboardButton.WithCallbackData("+30m", $"{CallbackPrefix}{sessionId}:+30"),
            }
        });
    }
}
