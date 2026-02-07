using Telegram.Bot;
using Telegram.Bot.Types;
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
    ITimeTrackingService timeTrackingService,
    ICommandParameterParser parameterParser)
{
    // Default UTC offset until user settings are implemented in Epic 5
    private const int DefaultUtcOffsetMinutes = 0;

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
            // Parse the timestamp from command parameters
            var timestamp = parameterParser.ParseTimestamp(messageText, DefaultUtcOffsetMinutes);

            // Execute the state transition
            var result = await timeTrackingService.StartStateAsync(
                userId.Value,
                targetState,
                timestamp,
                cancellationToken);

            // Send response based on result
            var responseText = FormatResponse(result, stateName);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: responseText,
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
        var session = result.StartedSession;
        var time = FormatTime(session.StartedAt);
        return $"Started {stateName} at {time}";
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

        var newTime = FormatTime(result.StartedSession.StartedAt);

        return $"Ended {previousStateName} ({duration}), started {stateName} at {newTime}";
    }

    /// <summary>
    /// Formats a message for when a session is ended (toggle behavior).
    /// </summary>
    private static string FormatSessionEnded(TrackingResult.SessionEnded result)
    {
        var session = result.EndedSession;
        var stateName = GetStateName(session.State);
        var duration = FormatDuration(session.StartedAt, session.EndedAt!.Value);

        return $"Ended {stateName}, duration: {duration}";
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
}
