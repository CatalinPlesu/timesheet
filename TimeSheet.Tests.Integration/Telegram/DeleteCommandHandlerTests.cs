using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types.ReplyMarkups;
using TimeSheet.Core.Application.Interfaces;
using TimeSheet.Tests.Integration.Base;
using TimeSheet.Tests.Integration.Fixtures;
using TimeSheet.Tests.Integration.Mocks;

namespace TimeSheet.Tests.Integration.TelegramTests;

/// <summary>
/// Integration tests for the /delete command handler.
/// Tests deleting tracking sessions with confirmation prompts.
/// </summary>
public class DeleteCommandHandlerTests(TelegramBotTestFixture fixture) : TelegramBotTestBase(fixture)
{
    [Fact]
    public async Task DeleteCommand_NoArgs_ShowsConfirmationForLastEntry()
    {
        // Arrange - create a tracking session
        const long userId = 30001;
        await RegisterTestUserAsync(telegramUserId: userId);
        await SendTextAsync("/work", userId: userId);
        await SendTextAsync("/work", userId: userId); // Stop it
        Fixture.MockBotClient.ClearResponses();

        // Act
        var responses = await SendTextAsync("/delete", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("Most recent entry:", responses[0].Text);
        Assert.Contains("Work session", responses[0].Text);
        Assert.Contains("Are you sure you want to delete this entry?", responses[0].Text);

        // Verify inline keyboard is present with Yes/No buttons
        Assert.NotNull(responses[0].ReplyMarkup);
        var keyboard = Assert.IsType<InlineKeyboardMarkup>(responses[0].ReplyMarkup);
        Assert.Single(keyboard.InlineKeyboard); // 1 row

        // First row: Yes, delete it / No, cancel
        var row = keyboard.InlineKeyboard.ElementAt(0).ToList();
        Assert.Equal(2, row.Count);
        Assert.Contains("Yes", row[0].Text);
        Assert.Contains("No", row[1].Text);
    }

    [Fact]
    public async Task DeleteCommand_WithId1_ShowsConfirmationForFirstEntry()
    {
        // Arrange - create multiple tracking sessions
        const long userId = 30002;
        await RegisterTestUserAsync(telegramUserId: userId);

        // Create first session (older)
        await SendTextAsync("/work", userId: userId);
        await Task.Delay(10); // Small delay to ensure different timestamps
        await SendTextAsync("/work", userId: userId);

        // Create second session (newer)
        await SendTextAsync("/lunch", userId: userId);
        await Task.Delay(10);
        await SendTextAsync("/lunch", userId: userId);

        Fixture.MockBotClient.ClearResponses();

        // Act - delete entry ID 1 (first entry from /list)
        var responses = await SendTextAsync("/delete 1", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("Entry #1:", responses[0].Text);
        Assert.Contains("Work session", responses[0].Text); // Should be the work session
        Assert.Contains("Are you sure you want to delete this entry?", responses[0].Text);
    }

    [Fact]
    public async Task DeleteCommand_NoEntries_ShowsErrorMessage()
    {
        // Arrange
        const long userId = 30003;
        await RegisterTestUserAsync(telegramUserId: userId);

        // Act - try to delete without any entries
        var responses = await SendTextAsync("/delete", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("don't have any tracking entries to delete", responses[0].Text);
    }

    [Fact]
    public async Task DeleteCommand_IdOutOfRange_ShowsErrorMessage()
    {
        // Arrange - create only one entry
        const long userId = 30004;
        await RegisterTestUserAsync(telegramUserId: userId);
        await SendTextAsync("/work", userId: userId);
        await SendTextAsync("/work", userId: userId);
        Fixture.MockBotClient.ClearResponses();

        // Act - try to delete entry ID 5 when only 1 exists
        var responses = await SendTextAsync("/delete 5", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("only have 1 entry today", responses[0].Text);
    }

    [Fact]
    public async Task DeleteCommand_InvalidId_ShowsErrorMessage()
    {
        // Arrange - create an entry
        const long userId = 30015;
        await RegisterTestUserAsync(telegramUserId: userId);
        await SendTextAsync("/work", userId: userId);
        await SendTextAsync("/work", userId: userId);
        Fixture.MockBotClient.ClearResponses();

        // Act - try to delete entry ID 0 (invalid)
        var responses = await SendTextAsync("/delete 0", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("Entry ID must be a positive number", responses[0].Text);
    }

    [Fact]
    public async Task DeleteCommand_WithId2_ShowsConfirmationForSecondEntry()
    {
        // Arrange - create multiple tracking sessions
        const long userId = 30016;
        await RegisterTestUserAsync(telegramUserId: userId);

        // Create three sessions
        await SendTextAsync("/work", userId: userId);
        await Task.Delay(10);
        await SendTextAsync("/work", userId: userId);

        await SendTextAsync("/lunch", userId: userId);
        await Task.Delay(10);
        await SendTextAsync("/lunch", userId: userId);

        await SendTextAsync("/commute", userId: userId);
        await Task.Delay(10);
        await SendTextAsync("/commute", userId: userId);

        Fixture.MockBotClient.ClearResponses();

        // Act - delete entry ID 2 (second entry from /list)
        var responses = await SendTextAsync("/delete 2", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("Entry #2:", responses[0].Text);
        Assert.Contains("Lunch break", responses[0].Text); // Should be the lunch session
        Assert.Contains("Are you sure you want to delete this entry?", responses[0].Text);
    }

    [Fact]
    public async Task CallbackQuery_ConfirmDelete_DeletesSession()
    {
        // Arrange - create a session to delete
        const long userId = 30005;
        await RegisterTestUserAsync(telegramUserId: userId);

        // Create a work session
        await SendTextAsync("/work", userId: userId);
        await Task.Delay(100); // Wait a bit
        await SendTextAsync("/work", userId: userId); // Stop it

        // Get the session ID
        Guid sessionId;
        using (var scope = Fixture.ServiceProvider.CreateScope())
        {
            var trackingSessionRepository = scope.ServiceProvider.GetRequiredService<ITrackingSessionRepository>();
            var sessions = await trackingSessionRepository.GetRecentSessionsAsync(userId, 1);
            sessionId = sessions[0].Id;
        }

        Fixture.MockBotClient.ClearResponses();

        // Act - send callback query to confirm deletion
        var responses = await SendCallbackQueryAsync($"delete:{sessionId}:confirm", userId: userId);

        // Assert - should have updated message and callback answer
        Assert.Equal(2, responses.Count);

        // First response: updated message showing deletion
        Assert.Equal(ResponseType.EditedMessage, responses[0].Type);
        Assert.Contains("Entry deleted:", responses[0].Text);
        Assert.Contains("Work session", responses[0].Text);

        // Second response: callback answer with feedback
        Assert.Equal(ResponseType.CallbackAnswer, responses[1].Type);
        Assert.Equal("✓ Entry deleted successfully", responses[1].Text);

        // Verify the session was actually deleted from the database
        using (var verifyScope = Fixture.ServiceProvider.CreateScope())
        {
            var trackingSessionRepository = verifyScope.ServiceProvider.GetRequiredService<ITrackingSessionRepository>();
            var sessions = await trackingSessionRepository.GetRecentSessionsAsync(userId, 10);
            Assert.Empty(sessions); // Should have no sessions
        }
    }

    [Fact]
    public async Task CallbackQuery_CancelDelete_DoesNotDeleteSession()
    {
        // Arrange - create a session
        const long userId = 30006;
        await RegisterTestUserAsync(telegramUserId: userId);

        await SendTextAsync("/work", userId: userId);
        await Task.Delay(100);
        await SendTextAsync("/work", userId: userId);

        // Get the session ID
        Guid sessionId;
        using (var scope = Fixture.ServiceProvider.CreateScope())
        {
            var trackingSessionRepository = scope.ServiceProvider.GetRequiredService<ITrackingSessionRepository>();
            var sessions = await trackingSessionRepository.GetRecentSessionsAsync(userId, 1);
            sessionId = sessions[0].Id;
        }

        Fixture.MockBotClient.ClearResponses();

        // Act - send callback query to cancel deletion
        var responses = await SendCallbackQueryAsync($"delete:{sessionId}:cancel", userId: userId);

        // Assert
        Assert.Equal(2, responses.Count);

        // First response: updated message showing cancellation
        Assert.Equal(ResponseType.EditedMessage, responses[0].Type);
        Assert.Contains("Deletion cancelled", responses[0].Text);

        // Second response: callback answer
        Assert.Equal(ResponseType.CallbackAnswer, responses[1].Type);
        Assert.Equal("❌ Deletion cancelled", responses[1].Text);

        // Verify the session still exists in the database
        using (var verifyScope = Fixture.ServiceProvider.CreateScope())
        {
            var trackingSessionRepository = verifyScope.ServiceProvider.GetRequiredService<ITrackingSessionRepository>();
            var sessions = await trackingSessionRepository.GetRecentSessionsAsync(userId, 1);
            Assert.Single(sessions); // Should still have the session
            Assert.Equal(sessionId, sessions[0].Id);
        }
    }

    [Fact]
    public async Task CallbackQuery_NonRegisteredUser_RejectsWithError()
    {
        // Arrange - don't register the user
        const long userId = 30007;

        // Act
        var responses = await SendCallbackQueryAsync("delete:some-id:confirm", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.CallbackAnswer, responses[0].Type);
        Assert.Contains("need to register first", responses[0].Text);
        Assert.True(responses[0].ShowAlert); // Should be shown as an alert
    }

    [Fact]
    public async Task CallbackQuery_InvalidSessionId_RejectsWithError()
    {
        // Arrange
        const long userId = 30008;
        await RegisterTestUserAsync(telegramUserId: userId);

        // Act - use an invalid session ID
        var responses = await SendCallbackQueryAsync("delete:invalid-guid:confirm", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.CallbackAnswer, responses[0].Type);
        Assert.Contains("Invalid session ID", responses[0].Text);
    }

    [Fact]
    public async Task CallbackQuery_NonExistentSession_RejectsWithError()
    {
        // Arrange
        const long userId = 30009;
        await RegisterTestUserAsync(telegramUserId: userId);
        var nonExistentId = Guid.NewGuid();

        // Act
        var responses = await SendCallbackQueryAsync($"delete:{nonExistentId}:confirm", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.CallbackAnswer, responses[0].Type);
        Assert.Contains("Session not found", responses[0].Text);
    }

    [Fact]
    public async Task CallbackQuery_OtherUsersSession_RejectsWithError()
    {
        // Arrange - create a session for user A
        const long userA = 30010;
        const long userB = 30011;

        await RegisterTestUserAsync(telegramUserId: userA);
        await RegisterTestUserAsync(telegramUserId: userB);

        await SendTextAsync("/work", userId: userA);
        await SendTextAsync("/work", userId: userA);

        // Get user A's session ID
        using var scope = Fixture.ServiceProvider.CreateScope();
        var trackingSessionRepository = scope.ServiceProvider.GetRequiredService<ITrackingSessionRepository>();
        var sessions = await trackingSessionRepository.GetRecentSessionsAsync(userA, 1);
        var sessionId = sessions[0].Id;

        // Act - user B tries to delete user A's session
        var responses = await SendCallbackQueryAsync($"delete:{sessionId}:confirm", userId: userB);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.CallbackAnswer, responses[0].Type);
        Assert.Contains("can only delete your own sessions", responses[0].Text);
    }

    [Fact]
    public async Task DeleteCommand_ActiveSession_ShowsOngoingStatus()
    {
        // Arrange - create an active (ongoing) session
        const long userId = 30012;
        await RegisterTestUserAsync(telegramUserId: userId);
        await SendTextAsync("/work", userId: userId); // Start but don't stop
        Fixture.MockBotClient.ClearResponses();

        // Act
        var responses = await SendTextAsync("/delete", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Contains("Ended: ongoing", responses[0].Text);
        Assert.Contains("Duration: ongoing", responses[0].Text);
    }

    [Fact]
    public async Task DeleteCommand_CommuteSession_ShowsDirection()
    {
        // Arrange - create a commute session
        const long userId = 30013;
        await RegisterTestUserAsync(telegramUserId: userId);
        await SendTextAsync("/commute", userId: userId);
        await SendTextAsync("/commute", userId: userId);
        Fixture.MockBotClient.ClearResponses();

        // Act
        var responses = await SendTextAsync("/delete", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Contains("Type: Commute", responses[0].Text);
    }

    [Fact]
    public async Task CallbackQuery_DeleteMultipleSessions_EachDeletionWorks()
    {
        // Arrange - create multiple sessions
        const long userId = 30014;
        await RegisterTestUserAsync(telegramUserId: userId);

        // Create three sessions
        await SendTextAsync("/work", userId: userId);
        await Task.Delay(10);
        await SendTextAsync("/work", userId: userId);

        await SendTextAsync("/lunch", userId: userId);
        await Task.Delay(10);
        await SendTextAsync("/lunch", userId: userId);

        await SendTextAsync("/commute", userId: userId);
        await Task.Delay(10);
        await SendTextAsync("/commute", userId: userId);

        // Get all session IDs
        List<Guid> sessionIds;
        using (var scope = Fixture.ServiceProvider.CreateScope())
        {
            var trackingSessionRepository = scope.ServiceProvider.GetRequiredService<ITrackingSessionRepository>();
            var sessions = await trackingSessionRepository.GetRecentSessionsAsync(userId, 10);
            Assert.Equal(3, sessions.Count);
            sessionIds = sessions.Select(s => s.Id).ToList();
        }

        // Act - delete first session
        Fixture.MockBotClient.ClearResponses();
        await SendCallbackQueryAsync($"delete:{sessionIds[0]}:confirm", userId: userId);

        // Assert - should have 2 sessions left
        using (var verifyScope1 = Fixture.ServiceProvider.CreateScope())
        {
            var trackingSessionRepository = verifyScope1.ServiceProvider.GetRequiredService<ITrackingSessionRepository>();
            var sessions = await trackingSessionRepository.GetRecentSessionsAsync(userId, 10);
            Assert.Equal(2, sessions.Count);
        }

        // Act - delete second session
        Fixture.MockBotClient.ClearResponses();
        await SendCallbackQueryAsync($"delete:{sessionIds[1]}:confirm", userId: userId);

        // Assert - should have 1 session left
        using (var verifyScope2 = Fixture.ServiceProvider.CreateScope())
        {
            var trackingSessionRepository = verifyScope2.ServiceProvider.GetRequiredService<ITrackingSessionRepository>();
            var sessions = await trackingSessionRepository.GetRecentSessionsAsync(userId, 10);
            Assert.Single(sessions);
        }

        // Act - delete third session
        Fixture.MockBotClient.ClearResponses();
        await SendCallbackQueryAsync($"delete:{sessionIds[2]}:confirm", userId: userId);

        // Assert - should have no sessions left
        using (var verifyScope3 = Fixture.ServiceProvider.CreateScope())
        {
            var trackingSessionRepository = verifyScope3.ServiceProvider.GetRequiredService<ITrackingSessionRepository>();
            var sessions = await trackingSessionRepository.GetRecentSessionsAsync(userId, 10);
            Assert.Empty(sessions);
        }
    }
}
