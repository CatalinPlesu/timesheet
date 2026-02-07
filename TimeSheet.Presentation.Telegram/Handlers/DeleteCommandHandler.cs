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
/// Handles the /delete command for deleting tracking session entries.
/// Supports deleting the most recent entry or selecting N entries back.
/// Requires confirmation before deletion for safety.
/// </summary>
public class DeleteCommandHandler(
    ILogger<DeleteCommandHandler> logger,
    IServiceScopeFactory serviceScopeFactory)
{
    // Callback data prefixes for inline keyboard buttons
    private const string CallbackPrefix = "delete:";
    private const string ConfirmAction = "confirm";
    private const string CancelAction = "cancel";

    /// <summary>
    /// Handles the /delete command.
    /// </summary>
    /// <param name="botClient">The Telegram bot client.</param>
    /// <param name="message">The incoming message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task HandleDeleteAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        var userId = message.From?.Id;
        if (userId == null)
        {
            logger.LogWarning("Received /delete message without user ID");
            return;
        }

        var messageText = message.Text ?? string.Empty;
        var parts = messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Parse the entry index (default to 0 for most recent)
        var entryIndex = 0;
        if (parts.Length > 1 && int.TryParse(parts[1], out var index))
        {
            entryIndex = index;
        }

        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var trackingSessionRepository = scope.ServiceProvider.GetRequiredService<ITrackingSessionRepository>();

            // Get recent sessions (we need entryIndex + 1 to access the Nth entry)
            var recentSessions = await trackingSessionRepository.GetRecentSessionsAsync(
                userId.Value,
                entryIndex + 1,
                cancellationToken);

            if (recentSessions.Count <= entryIndex)
            {
                var errorMessage = entryIndex == 0
                    ? "You don't have any tracking entries to delete."
                    : $"You don't have {entryIndex + 1} tracking entries. You only have {recentSessions.Count}.";

                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: errorMessage,
                    cancellationToken: cancellationToken);
                return;
            }

            var sessionToDelete = recentSessions[entryIndex];

            // Format the session details
            var sessionDetails = FormatSessionDetails(sessionToDelete, entryIndex);

            // Create inline keyboard with confirmation buttons
            var keyboard = CreateConfirmationKeyboard(sessionToDelete.Id);

            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: sessionDetails,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "User {UserId} requested deletion confirmation for session {SessionId} (index {Index})",
                userId.Value,
                sessionToDelete.Id,
                entryIndex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling /delete command for user {UserId}", userId.Value);
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
            // Not a delete callback
            return;
        }

        try
        {
            // Parse callback data: "delete:{sessionId}:{action}"
            // Example: "delete:abc123:confirm" means confirm deletion of session abc123
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
            var action = parts[2];

            if (!Guid.TryParse(sessionIdStr, out var sessionId))
            {
                logger.LogWarning("Invalid session ID in callback data: {SessionId}", sessionIdStr);
                await botClient.AnswerCallbackQuery(
                    callbackQueryId: callbackQuery.Id,
                    text: "Invalid session ID",
                    cancellationToken: cancellationToken);
                return;
            }

            // Handle cancel action
            if (action == CancelAction)
            {
                await HandleCancelAsync(botClient, callbackQuery, cancellationToken);
                return;
            }

            // Handle confirm action
            if (action == ConfirmAction)
            {
                await HandleConfirmAsync(botClient, callbackQuery, sessionId, userId, cancellationToken);
                return;
            }

            logger.LogWarning("Unknown action in callback data: {Action}", action);
            await botClient.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: "Invalid action",
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling delete callback query for user {UserId}", userId);
            await botClient.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: "An error occurred while processing your request",
                showAlert: true,
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Handles the cancel action.
    /// </summary>
    private async Task HandleCancelAsync(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        // Update the message to show cancellation
        if (callbackQuery.Message != null)
        {
            await botClient.EditMessageText(
                chatId: callbackQuery.Message.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: "Deletion cancelled.",
                cancellationToken: cancellationToken);
        }

        await botClient.AnswerCallbackQuery(
            callbackQueryId: callbackQuery.Id,
            text: "Cancelled",
            cancellationToken: cancellationToken);

        logger.LogInformation(
            "User {UserId} cancelled deletion",
            callbackQuery.From.Id);
    }

    /// <summary>
    /// Handles the confirm action.
    /// </summary>
    private async Task HandleConfirmAsync(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        Guid sessionId,
        long userId,
        CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var trackingSessionRepository = scope.ServiceProvider.GetRequiredService<ITrackingSessionRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

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
                "User {UserId} attempted to delete session {SessionId} owned by {OwnerId}",
                userId,
                sessionId,
                session.UserId);
            await botClient.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: "You can only delete your own sessions",
                cancellationToken: cancellationToken);
            return;
        }

        // Delete the session
        trackingSessionRepository.Remove(session);
        await unitOfWork.CompleteAsync(cancellationToken);

        // Update the message to show successful deletion
        if (callbackQuery.Message != null)
        {
            var deletionSummary = FormatDeletionSummary(session);
            await botClient.EditMessageText(
                chatId: callbackQuery.Message.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: deletionSummary,
                cancellationToken: cancellationToken);
        }

        await botClient.AnswerCallbackQuery(
            callbackQueryId: callbackQuery.Id,
            text: "Deleted",
            cancellationToken: cancellationToken);

        logger.LogInformation(
            "User {UserId} deleted session {SessionId}",
            userId,
            sessionId);
    }

    /// <summary>
    /// Formats the session details for the confirmation prompt.
    /// </summary>
    private static string FormatSessionDetails(TrackingSession session, int index)
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

        var entryLabel = index == 0 ? "Most recent entry" : $"Entry {index} back";

        var startTime = session.StartedAt.ToString("yyyy-MM-dd HH:mm");
        var endTime = session.EndedAt?.ToString("HH:mm") ?? "ongoing";

        var duration = session.EndedAt.HasValue
            ? FormatDuration(session.StartedAt, session.EndedAt.Value)
            : "ongoing";

        return $"{entryLabel}:\n" +
               $"Type: {stateDisplay}\n" +
               $"Started: {startTime}\n" +
               $"Ended: {endTime}\n" +
               $"Duration: {duration}\n\n" +
               $"Are you sure you want to delete this entry?";
    }

    /// <summary>
    /// Formats the deletion summary message.
    /// </summary>
    private static string FormatDeletionSummary(TrackingSession session)
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

        return $"Entry deleted:\n" +
               $"Type: {stateDisplay}\n" +
               $"Started: {startTime}";
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
    /// Creates an inline keyboard with confirmation buttons (Yes/No).
    /// </summary>
    private static InlineKeyboardMarkup CreateConfirmationKeyboard(Guid sessionId)
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Yes, delete it", $"{CallbackPrefix}{sessionId}:{ConfirmAction}"),
                InlineKeyboardButton.WithCallbackData("No, cancel", $"{CallbackPrefix}{sessionId}:{CancelAction}"),
            }
        });
    }
}
