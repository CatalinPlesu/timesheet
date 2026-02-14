using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TimeSheet.Core.Application.Interfaces.Services;
using TimeSheet.Core.Application.Interfaces.Persistence;
using TimeSheet.Core.Domain.Repositories;
using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.Enums;
using TimeSheet.Infrastructure.Persistence;

namespace TimeSheet.Presentation.Telegram.Handlers;

/// <summary>
/// Handles the /delete command for deleting tracking session entries.
/// Supports deleting the most recent entry (/delete) or a specific entry by ID (/delete [id]).
/// Entry IDs come from /list command output (1, 2, 3, ...).
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

        // Parse the entry ID (default to null for most recent)
        int? entryId = null;
        if (parts.Length > 1 && int.TryParse(parts[1], out var id))
        {
            entryId = id;
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

            List<TrackingSession> sessions;
            TrackingSession sessionToDelete;

            if (entryId.HasValue)
            {
                // ID-based deletion: get today's sessions and select by ID
                if (entryId.Value < 1)
                {
                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: "Entry ID must be a positive number (1, 2, 3, ...).",
                        cancellationToken: cancellationToken);
                    return;
                }

                var today = DateTime.UtcNow.Date;
                sessions = await timeTrackingService.GetTodaysSessionsAsync(
                    userId.Value,
                    today,
                    cancellationToken);

                if (sessions.Count == 0)
                {
                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: "You don't have any tracking entries for today.",
                        cancellationToken: cancellationToken);
                    return;
                }

                if (entryId.Value > sessions.Count)
                {
                    var errorMessage = sessions.Count == 1
                        ? "You only have 1 entry today. Use /delete 1 to delete it."
                        : $"You only have {sessions.Count} entries today. Entry ID must be between 1 and {sessions.Count}.";

                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: errorMessage,
                        cancellationToken: cancellationToken);
                    return;
                }

                // Convert 1-based ID to 0-based index
                sessionToDelete = sessions[entryId.Value - 1];
            }
            else
            {
                // No ID provided: delete most recent entry
                var trackingSessionRepository = scope.ServiceProvider.GetRequiredService<ITrackingSessionRepository>();
                sessions = await trackingSessionRepository.GetRecentSessionsAsync(
                    userId.Value,
                    1,
                    cancellationToken);

                if (sessions.Count == 0)
                {
                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: "You don't have any tracking entries to delete.",
                        cancellationToken: cancellationToken);
                    return;
                }

                sessionToDelete = sessions[0];
            }

            // Format the session details
            var sessionDetails = FormatSessionDetails(sessionToDelete, entryId, user.UtcOffsetMinutes);

            // Create inline keyboard with confirmation buttons
            var keyboard = CreateConfirmationKeyboard(sessionToDelete.Id);

            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: sessionDetails,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "User {UserId} requested deletion confirmation for session {SessionId} (entry ID: {EntryId})",
                userId.Value,
                sessionToDelete.Id,
                entryId?.ToString() ?? "most recent");
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
            text: "❌ Deletion cancelled",
            showAlert: false, // Small popup
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
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        // Get user for timezone offset
        var user = await userRepository.GetByTelegramUserIdAsync(userId, cancellationToken);
        if (user == null)
        {
            logger.LogWarning("User {UserId} not found", userId);
            await botClient.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: "User not found",
                cancellationToken: cancellationToken);
            return;
        }

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
            var deletionSummary = FormatDeletionSummary(session, user.UtcOffsetMinutes);
            await botClient.EditMessageText(
                chatId: callbackQuery.Message.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: deletionSummary,
                cancellationToken: cancellationToken);
        }

        await botClient.AnswerCallbackQuery(
            callbackQueryId: callbackQuery.Id,
            text: "✓ Entry deleted successfully",
            showAlert: false, // Small popup
            cancellationToken: cancellationToken);

        logger.LogInformation(
            "User {UserId} deleted session {SessionId}",
            userId,
            sessionId);
    }

    /// <summary>
    /// Formats the session details for the confirmation prompt, converting timestamps to user's local timezone.
    /// </summary>
    private static string FormatSessionDetails(TrackingSession session, int? entryId, int utcOffsetMinutes)
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

        var entryLabel = entryId.HasValue ? $"Entry #{entryId.Value}" : "Most recent entry";

        var localStartedAt = session.StartedAt.AddMinutes(utcOffsetMinutes);
        var startTime = localStartedAt.ToString("yyyy-MM-dd HH:mm");
        var endTime = session.EndedAt.HasValue
            ? session.EndedAt.Value.AddMinutes(utcOffsetMinutes).ToString("HH:mm")
            : "ongoing";

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
    /// Formats the deletion summary message, converting timestamps to user's local timezone.
    /// </summary>
    private static string FormatDeletionSummary(TrackingSession session, int utcOffsetMinutes)
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

        var localStartedAt = session.StartedAt.AddMinutes(utcOffsetMinutes);
        var startTime = localStartedAt.ToString("yyyy-MM-dd HH:mm");

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
