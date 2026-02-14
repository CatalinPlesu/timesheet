using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TimeSheet.Core.Application.Interfaces;
using TimeSheet.Core.Application.Models;
using TimeSheet.Core.Application.Parsers;
using TimeSheet.Core.Domain.Enums;

namespace TimeSheet.Presentation.Telegram.Handlers;

/// <summary>
/// Handles tracking commands (/commute, /work, /lunch) from Telegram users.
/// </summary>
public class TrackingCommandHandler(
    ILogger<TrackingCommandHandler> logger,
    IServiceScopeFactory serviceScopeFactory,
    ICommandParameterParser parameterParser)
{
    // Callback data prefix for inline keyboard buttons
    private const string CallbackPrefix = "track:";

    /// <summary>
    /// Handles the /commute command.
    /// </summary>
    public async Task HandleCommuteAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        await HandleTrackingCommandAsync(
            botClient,
            message,
            TrackingState.Commuting,
            "commuting",
            cancellationToken);
    }

    /// <summary>
    /// Handles the /work command.
    /// </summary>
    public async Task HandleWorkAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        await HandleTrackingCommandAsync(
            botClient,
            message,
            TrackingState.Working,
            "working",
            cancellationToken);
    }

    /// <summary>
    /// Handles the /lunch command.
    /// </summary>
    public async Task HandleLunchAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        await HandleTrackingCommandAsync(
            botClient,
            message,
            TrackingState.Lunch,
            "on lunch break",
            cancellationToken);
    }

    /// <summary>
    /// Handles callback queries from predictive action buttons.
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
            // Not a tracking callback
            return;
        }

        try
        {
            // Parse callback data: "track:{state}"
            // Example: "track:work" means start/toggle work state
            var parts = data.Split(':');
            if (parts.Length != 2)
            {
                logger.LogWarning("Invalid callback data format: {Data}", data);
                await botClient.AnswerCallbackQuery(
                    callbackQueryId: callbackQuery.Id,
                    text: "Invalid button data",
                    cancellationToken: cancellationToken);
                return;
            }

            var stateStr = parts[1];
            var targetState = stateStr.ToLowerInvariant() switch
            {
                "commute" => TrackingState.Commuting,
                "work" => TrackingState.Working,
                "lunch" => TrackingState.Lunch,
                _ => (TrackingState?)null
            };

            if (targetState == null)
            {
                logger.LogWarning("Invalid state in callback data: {State}", stateStr);
                await botClient.AnswerCallbackQuery(
                    callbackQueryId: callbackQuery.Id,
                    text: "Invalid state",
                    cancellationToken: cancellationToken);
                return;
            }

            using var scope = serviceScopeFactory.CreateScope();
            var timeTrackingService = scope.ServiceProvider.GetRequiredService<ITimeTrackingService>();
            var userSettingsService = scope.ServiceProvider.GetRequiredService<IUserSettingsService>();

            // Get the user's UTC offset from settings
            var user = await userSettingsService.GetUserAsync(userId, cancellationToken);
            if (user == null)
            {
                logger.LogWarning("User {UserId} not found in database", userId);
                await botClient.AnswerCallbackQuery(
                    callbackQueryId: callbackQuery.Id,
                    text: "User not found",
                    showAlert: true,
                    cancellationToken: cancellationToken);
                return;
            }

            // Execute the state transition (using current time, no parameter parsing needed)
            var result = await timeTrackingService.StartStateAsync(
                userId,
                targetState.Value,
                DateTime.UtcNow,
                cancellationToken);

            // Format the response based on result
            var stateName = targetState.Value switch
            {
                TrackingState.Commuting => "commuting",
                TrackingState.Working => "working",
                TrackingState.Lunch => "on lunch break",
                _ => targetState.Value.ToString().ToLowerInvariant()
            };

            var responseText = FormatResponse(result, stateName);
            var keyboard = CreatePredictiveActionButtons(result, user.UtcOffsetMinutes);

            // Update the message with the new status
            if (callbackQuery.Message != null)
            {
                await botClient.EditMessageText(
                    chatId: callbackQuery.Message.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: responseText,
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken);
            }

            // Answer the callback query with feedback
            var feedbackText = result switch
            {
                TrackingResult.SessionStarted => $"Started {stateName}",
                TrackingResult.SessionEnded => $"Stopped {stateName}",
                _ => "Status updated"
            };

            await botClient.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: feedbackText,
                showAlert: false,
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "User {UserId} executed {State} via button: {ResultType}",
                userId,
                targetState.Value,
                result.GetType().Name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling tracking callback query for user {UserId}", userId);
            await botClient.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: "An error occurred while processing your request",
                showAlert: true,
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Common handler for all tracking commands.
    /// </summary>
    private async Task HandleTrackingCommandAsync(
        ITelegramBotClient botClient,
        Message message,
        TrackingState targetState,
        string stateName,
        CancellationToken cancellationToken)
    {
        var userId = message.From?.Id;
        if (userId == null)
        {
            logger.LogWarning("Received message without user ID");
            return;
        }

        var messageText = message.Text ?? string.Empty;

        try
        {
            // Create a scope to resolve scoped services (ITimeTrackingService uses DbContext)
            using var scope = serviceScopeFactory.CreateScope();
            var timeTrackingService = scope.ServiceProvider.GetRequiredService<ITimeTrackingService>();
            var userSettingsService = scope.ServiceProvider.GetRequiredService<IUserSettingsService>();

            // Get the user's UTC offset from settings
            var user = await userSettingsService.GetUserAsync(userId.Value, cancellationToken);
            if (user == null)
            {
                logger.LogWarning("User {UserId} not found in database", userId.Value);
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "User not found. Please register first.",
                    cancellationToken: cancellationToken);
                return;
            }

            // Parse the timestamp from command parameters using the user's UTC offset
            var timestamp = parameterParser.ParseTimestamp(messageText, user.UtcOffsetMinutes);

            // Execute the state transition
            var result = await timeTrackingService.StartStateAsync(
                userId.Value,
                targetState,
                timestamp,
                cancellationToken);

            // Send response based on result
            var responseText = FormatResponse(result, stateName);
            var keyboard = CreatePredictiveActionButtons(result, user.UtcOffsetMinutes);

            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: responseText,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "User {UserId} executed {State} command: {ResultType}",
                userId.Value,
                targetState,
                result.GetType().Name);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid command parameter from user {UserId}", userId.Value);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: $"Invalid parameter: {ex.Message}",
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling tracking command for user {UserId}", userId.Value);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "An error occurred while processing your request. Please try again.",
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Formats the response message based on the tracking result.
    /// </summary>
    private static string FormatResponse(TrackingResult result, string stateName)
    {
        return result switch
        {
            TrackingResult.SessionStarted started when started.EndedSession != null =>
                FormatSessionStartedWithPreviousEnded(started, stateName),

            TrackingResult.SessionStarted started =>
                FormatSessionStarted(started, stateName),

            TrackingResult.SessionEnded ended =>
                FormatSessionEnded(ended),

            TrackingResult.NoChange =>
                "No active session to end.",

            _ => "Unknown result."
        };
    }

    /// <summary>
    /// Formats a message for when a new session is started.
    /// </summary>
    private static string FormatSessionStarted(TrackingResult.SessionStarted result, string stateName)
    {
        return $"Started tracking {stateName}";
    }

    /// <summary>
    /// Formats a message for when a new session is started and a previous one was ended.
    /// </summary>
    private static string FormatSessionStartedWithPreviousEnded(
        TrackingResult.SessionStarted result,
        string stateName)
    {
        var previousSession = result.EndedSession!;
        var previousStateName = GetStateName(previousSession.State);
        var duration = FormatDuration(previousSession.StartedAt, previousSession.EndedAt!.Value);

        return $"Stopped {previousStateName} ({duration}), started tracking {stateName}";
    }

    /// <summary>
    /// Formats a message for when a session is ended (toggle behavior).
    /// </summary>
    private static string FormatSessionEnded(TrackingResult.SessionEnded result)
    {
        var session = result.EndedSession;
        var stateName = GetStateName(session.State);
        var duration = FormatDuration(session.StartedAt, session.EndedAt!.Value);

        return $"Stopped {stateName}, duration: {duration}";
    }

    /// <summary>
    /// Gets a user-friendly name for a tracking state.
    /// </summary>
    private static string GetStateName(TrackingState state)
    {
        return state switch
        {
            TrackingState.Commuting => "commuting",
            TrackingState.Working => "work session",
            TrackingState.Lunch => "lunch break",
            TrackingState.Idle => "idle",
            _ => state.ToString().ToLowerInvariant()
        };
    }

    /// <summary>
    /// Formats a UTC timestamp for display.
    /// For now, just shows the time in UTC. Will use user's timezone in Epic 5.
    /// </summary>
    private static string FormatTime(DateTime utcTime)
    {
        // TODO: Apply user's UTC offset when user settings are implemented
        return utcTime.ToString("HH:mm");
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
    /// Creates context-aware inline keyboard buttons for predicted next actions.
    /// </summary>
    private static InlineKeyboardMarkup? CreatePredictiveActionButtons(TrackingResult result, int utcOffsetMinutes)
    {
        return result switch
        {
            // When commute starts, suggest stopping commute or starting work
            TrackingResult.SessionStarted { StartedSession.State: TrackingState.Commuting } =>
                new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Stop Commute", $"{CallbackPrefix}commute"),
                        InlineKeyboardButton.WithCallbackData("Start Work", $"{CallbackPrefix}work")
                    }
                }),

            // When work starts, suggest lunch or stopping work (context-aware based on time)
            TrackingResult.SessionStarted { StartedSession.State: TrackingState.Working } started =>
                CreateWorkStartedButtons(started.StartedSession.StartedAt, utcOffsetMinutes),

            // When lunch starts, suggest returning to work
            TrackingResult.SessionStarted { StartedSession.State: TrackingState.Lunch } =>
                new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Back to Work", $"{CallbackPrefix}work")
                    }
                }),

            // When commute ends, suggest starting work
            TrackingResult.SessionEnded { EndedSession.State: TrackingState.Commuting } =>
                new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Start Work", $"{CallbackPrefix}work")
                    }
                }),

            // When work ends, suggest commute home
            TrackingResult.SessionEnded { EndedSession.State: TrackingState.Working } =>
                new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Commute Home", $"{CallbackPrefix}commute")
                    }
                }),

            // When lunch ends, suggest work (though this shouldn't happen with toggle logic)
            TrackingResult.SessionEnded { EndedSession.State: TrackingState.Lunch } =>
                new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Start Work", $"{CallbackPrefix}work")
                    }
                }),

            _ => null
        };
    }

    /// <summary>
    /// Creates context-aware buttons when work is started.
    /// Considers the time of day to prioritize lunch or end of work.
    /// </summary>
    private static InlineKeyboardMarkup CreateWorkStartedButtons(DateTime workStartTime, int utcOffsetMinutes)
    {
        // Convert to user's local time
        var localTime = workStartTime.AddMinutes(utcOffsetMinutes);
        var hour = localTime.Hour;

        // If it's around lunchtime (11:00-14:00), prioritize lunch button
        if (hour >= 11 && hour < 14)
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Take Lunch", $"{CallbackPrefix}lunch"),
                    InlineKeyboardButton.WithCallbackData("Stop Work", $"{CallbackPrefix}work")
                }
            });
        }

        // Otherwise, show general options
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Stop Work", $"{CallbackPrefix}work")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Take Lunch", $"{CallbackPrefix}lunch"),
                InlineKeyboardButton.WithCallbackData("Commute Home", $"{CallbackPrefix}commute")
            }
        });
    }
}
