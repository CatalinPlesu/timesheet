using Telegram.Bot;
using Telegram.Bot.Types;
using TimeSheet.Core.Application.Interfaces.Persistence;
using TimeSheet.Core.Domain.Repositories;

namespace TimeSheet.Presentation.Telegram.Handlers;

/// <summary>
/// Handles the /note command for adding or clearing notes on tracking sessions.
/// Syntax:
///   /note some text        — add note to most recent entry
///   /note 2 some text      — add note to 2nd most recent entry
///   /note clear            — clear note on most recent entry
///   /note 2 clear          — clear note on 2nd most recent entry
/// </summary>
public class NoteCommandHandler(
    ILogger<NoteCommandHandler> logger,
    IServiceScopeFactory serviceScopeFactory)
{
    /// <summary>
    /// Handles the /note command.
    /// </summary>
    public async Task HandleNoteAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        var userId = message.From?.Id;
        if (userId == null)
        {
            logger.LogWarning("Received /note message without user ID");
            return;
        }

        var messageText = message.Text ?? string.Empty;
        var parts = messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // parts[0] = "/note", parse rest
        // Determine N and note text
        int n = 1;
        string? noteText;

        if (parts.Length < 2)
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Usage: /note [N] <text|clear>\n  /note some text  — add note to most recent entry\n  /note 2 some text  — add note to 2nd most recent\n  /note clear  — clear note on most recent entry",
                cancellationToken: cancellationToken);
            return;
        }

        // Check if second token is an integer (entry index)
        if (int.TryParse(parts[1], out var parsedN) && parsedN > 0)
        {
            n = parsedN;
            // Rest of the parts after the index form the note text
            noteText = parts.Length > 2 ? string.Join(' ', parts[2..]) : null;
        }
        else
        {
            // No index — rest of tokens (from parts[1] onward) form the note text
            noteText = string.Join(' ', parts[1..]);
        }

        if (string.IsNullOrWhiteSpace(noteText))
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Please provide note text or 'clear'.",
                cancellationToken: cancellationToken);
            return;
        }

        // Determine if clearing
        bool clearing = noteText.Equals("clear", StringComparison.OrdinalIgnoreCase);
        string? finalNote = clearing ? null : noteText;

        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var trackingSessionRepository = scope.ServiceProvider.GetRequiredService<ITrackingSessionRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            // Fetch N most recent sessions, take the Nth one
            var recentSessions = await trackingSessionRepository.GetRecentSessionsAsync(
                userId.Value,
                n,
                cancellationToken);

            if (recentSessions.Count < n)
            {
                var available = recentSessions.Count;
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: available == 0
                        ? "You don't have any tracking entries."
                        : $"You only have {available} entr{(available == 1 ? "y" : "ies")}.",
                    cancellationToken: cancellationToken);
                return;
            }

            var session = recentSessions[n - 1];
            session.UpdateNote(finalNote);
            trackingSessionRepository.Update(session);
            await unitOfWork.CompleteAsync(cancellationToken);

            var stateDisplay = session.State switch
            {
                TimeSheet.Core.Domain.Enums.TrackingState.Commuting => "commute",
                TimeSheet.Core.Domain.Enums.TrackingState.Working => "work",
                TimeSheet.Core.Domain.Enums.TrackingState.Lunch => "lunch",
                _ => session.State.ToString().ToLowerInvariant()
            };

            var reply = clearing
                ? $"Note cleared on {stateDisplay} entry ({session.StartedAt:HH:mm})."
                : $"Note saved on {stateDisplay} entry ({session.StartedAt:HH:mm}).";

            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: reply,
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "User {UserId} {Action} note on session {SessionId}",
                userId.Value,
                clearing ? "cleared" : "set",
                session.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling /note command for user {UserId}", userId.Value);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "An error occurred while saving the note. Please try again.",
                cancellationToken: cancellationToken);
        }
    }
}
