using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types.ReplyMarkups;
using TimeSheet.Core.Application.Interfaces.Services;
using TimeSheet.Core.Application.Interfaces.Persistence;
using TimeSheet.Core.Domain.Repositories;
using TimeSheet.Tests.Integration.Base;
using TimeSheet.Tests.Integration.Fixtures;
using TimeSheet.Tests.Integration.Mocks;

namespace TimeSheet.Tests.Integration.TelegramTests;

/// <summary>
/// Integration tests for the /edit command handler.
/// Tests editing tracking sessions with inline keyboard adjustments.
/// </summary>
public class EditCommandHandlerTests(TelegramBotTestFixture fixture) : TelegramBotTestBase(fixture)
{
    [Fact]
    public async Task EditCommand_NoArgs_EditsLastEntry()
    {
        // Arrange - create a tracking session
        const long userId = 20001;
        await RegisterTestUserAsync(telegramUserId: userId);
        await SendTextAsync("/work", userId: userId);
        await SendTextAsync("/work", userId: userId); // Stop it
        Fixture.MockBotClient.ClearResponses();

        // Act
        var responses = await SendTextAsync("/edit", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("Most recent entry:", responses[0].Text);
        Assert.Contains("Work session", responses[0].Text);

        // Verify inline keyboard is present with start and end rows
        Assert.NotNull(responses[0].ReplyMarkup);
        var keyboard = Assert.IsType<InlineKeyboardMarkup>(responses[0].ReplyMarkup);
        Assert.Equal(2, keyboard.InlineKeyboard.Count()); // 2 rows (start + end)

        // First row: Start label + adjustment buttons
        var startRow = keyboard.InlineKeyboard.ElementAt(0).ToList();
        Assert.Equal(7, startRow.Count);
        Assert.Equal("Start:", startRow[0].Text);
        Assert.Equal("-30m", startRow[1].Text);
        Assert.Equal("+30m", startRow[6].Text);

        // Second row: End label + adjustment buttons
        var endRow = keyboard.InlineKeyboard.ElementAt(1).ToList();
        Assert.Equal(7, endRow.Count);
        Assert.Equal("End:", endRow[0].Text);
    }

    [Fact]
    public async Task EditCommand_WithId1_EditsFirstEntry()
    {
        // Arrange - create multiple tracking sessions today
        const long userId = 20002;
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

        // Act - edit entry 1 (first entry from today's list)
        var responses = await SendTextAsync("/edit 1", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("Entry #1:", responses[0].Text);
        Assert.Contains("Work session", responses[0].Text); // Should be the work session (first chronologically)
    }

    [Fact]
    public async Task EditCommand_NoEntries_ShowsErrorMessage()
    {
        // Arrange
        const long userId = 20003;
        await RegisterTestUserAsync(telegramUserId: userId);

        // Act - try to edit without any entries
        var responses = await SendTextAsync("/edit", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("don't have any tracking entries to edit", responses[0].Text);
    }

    [Fact]
    public async Task EditCommand_IdOutOfRange_ShowsErrorMessage()
    {
        // Arrange - create only one entry
        const long userId = 20004;
        await RegisterTestUserAsync(telegramUserId: userId);
        await SendTextAsync("/work", userId: userId);
        await SendTextAsync("/work", userId: userId);
        Fixture.MockBotClient.ClearResponses();

        // Act - try to edit entry 5 when only 1 exists
        var responses = await SendTextAsync("/edit 5", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("Entry ID 5 not found", responses[0].Text);
        Assert.Contains("You have 1 entries today", responses[0].Text);
        Assert.Contains("Use /list to see all entries", responses[0].Text);
    }

    [Fact]
    public async Task CallbackQuery_AdjustStartEarlier_UpdatesSession()
    {
        // Arrange - create a session to edit
        const long userId = 20005;
        await RegisterTestUserAsync(telegramUserId: userId);

        // Create a work session
        await SendTextAsync("/work", userId: userId);
        await Task.Delay(100); // Wait a bit
        await SendTextAsync("/work", userId: userId); // Stop it

        // Get the session ID
        Guid sessionId;
        DateTime originalStartTime;
        DateTime originalEndTime;

        using (var scope = Fixture.ServiceProvider.CreateScope())
        {
            var trackingSessionRepository = scope.ServiceProvider.GetRequiredService<ITrackingSessionRepository>();
            var sessions = await trackingSessionRepository.GetRecentSessionsAsync(userId, 1);
            sessionId = sessions[0].Id;
            originalStartTime = sessions[0].StartedAt;
            originalEndTime = sessions[0].EndedAt!.Value;
        }

        Fixture.MockBotClient.ClearResponses();

        // Act - move start time earlier by 5 minutes (safe: won't exceed end)
        var responses = await SendCallbackQueryAsync($"edit:{sessionId}:s:-5", userId: userId);

        // Assert - should have updated message and callback answer
        Assert.Equal(2, responses.Count);

        // First response: updated message
        Assert.Equal(ResponseType.EditedMessage, responses[0].Type);
        Assert.Contains("Editing entry:", responses[0].Text);

        // Second response: callback answer with feedback
        Assert.Equal(ResponseType.CallbackAnswer, responses[1].Type);
        Assert.Equal("Adjusted start by -5m", responses[1].Text);

        // Verify only the start time was updated in the database
        using (var verifyScope = Fixture.ServiceProvider.CreateScope())
        {
            var trackingSessionRepository = verifyScope.ServiceProvider.GetRequiredService<ITrackingSessionRepository>();
            var updatedSessions = await trackingSessionRepository.GetRecentSessionsAsync(userId, 1);
            var updatedSession = updatedSessions[0];
            Assert.Equal(originalStartTime.AddMinutes(-5), updatedSession.StartedAt);
            Assert.Equal(originalEndTime, updatedSession.EndedAt); // End time unchanged
        }
    }

    [Fact]
    public async Task CallbackQuery_AdjustEndLater_UpdatesSession()
    {
        // Arrange - create a session to edit
        const long userId = 20006;
        await RegisterTestUserAsync(telegramUserId: userId);

        await SendTextAsync("/work", userId: userId);
        await Task.Delay(100);
        await SendTextAsync("/work", userId: userId);

        // Get the session ID
        Guid sessionId;
        DateTime originalStartTime;
        DateTime originalEndTime;

        using (var scope = Fixture.ServiceProvider.CreateScope())
        {
            var trackingSessionRepository = scope.ServiceProvider.GetRequiredService<ITrackingSessionRepository>();
            var sessions = await trackingSessionRepository.GetRecentSessionsAsync(userId, 1);
            sessionId = sessions[0].Id;
            originalStartTime = sessions[0].StartedAt;
            originalEndTime = sessions[0].EndedAt!.Value;
        }

        Fixture.MockBotClient.ClearResponses();

        // Act - move end time later by 30 minutes (safe: won't go before start)
        var responses = await SendCallbackQueryAsync($"edit:{sessionId}:e:+30", userId: userId);

        // Assert
        Assert.Equal(2, responses.Count);
        Assert.Equal(ResponseType.EditedMessage, responses[0].Type);
        Assert.Equal(ResponseType.CallbackAnswer, responses[1].Type);
        Assert.Equal("Adjusted end by +30m", responses[1].Text);

        // Verify only end time was updated - use fresh scope
        using (var verifyScope = Fixture.ServiceProvider.CreateScope())
        {
            var trackingSessionRepository = verifyScope.ServiceProvider.GetRequiredService<ITrackingSessionRepository>();
            var updatedSessions = await trackingSessionRepository.GetRecentSessionsAsync(userId, 1);
            var updatedSession = updatedSessions[0];
            Assert.Equal(originalStartTime, updatedSession.StartedAt); // Start unchanged
            Assert.Equal(originalEndTime.AddMinutes(30), updatedSession.EndedAt); // End adjusted
        }
    }

    [Fact]
    public async Task CallbackQuery_MultipleClicks_AccumulateAdjustments()
    {
        // Arrange - create a session to edit
        const long userId = 20007;
        await RegisterTestUserAsync(telegramUserId: userId);

        await SendTextAsync("/work", userId: userId);
        await Task.Delay(100);
        await SendTextAsync("/work", userId: userId);

        Guid sessionId;
        DateTime originalStartTime;

        using (var scope = Fixture.ServiceProvider.CreateScope())
        {
            var trackingSessionRepository = scope.ServiceProvider.GetRequiredService<ITrackingSessionRepository>();
            var sessions = await trackingSessionRepository.GetRecentSessionsAsync(userId, 1);
            sessionId = sessions[0].Id;
            originalStartTime = sessions[0].StartedAt;
        }

        Fixture.MockBotClient.ClearResponses();

        // Act - click start -5m three times (moving start earlier is always safe)
        await SendCallbackQueryAsync($"edit:{sessionId}:s:-5", userId: userId);
        await Task.Delay(50); // Allow DB to commit
        Fixture.MockBotClient.ClearResponses();

        await SendCallbackQueryAsync($"edit:{sessionId}:s:-5", userId: userId);
        await Task.Delay(50); // Allow DB to commit
        Fixture.MockBotClient.ClearResponses();

        await SendCallbackQueryAsync($"edit:{sessionId}:s:-5", userId: userId);
        await Task.Delay(50); // Allow DB to commit

        // Assert - total adjustment should be -15 minutes - use fresh scope
        using (var verifyScope = Fixture.ServiceProvider.CreateScope())
        {
            var trackingSessionRepository = verifyScope.ServiceProvider.GetRequiredService<ITrackingSessionRepository>();
            var updatedSessions = await trackingSessionRepository.GetRecentSessionsAsync(userId, 1);
            var updatedSession = updatedSessions[0];
            Assert.Equal(originalStartTime.AddMinutes(-15), updatedSession.StartedAt);
        }
    }

    [Fact]
    public async Task CallbackQuery_NonRegisteredUser_RejectsWithError()
    {
        // Arrange - don't register the user
        const long userId = 20008;

        // Act
        var responses = await SendCallbackQueryAsync("edit:some-id:s:+5", userId: userId);

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
        const long userId = 20009;
        await RegisterTestUserAsync(telegramUserId: userId);

        // Act - use an invalid session ID
        var responses = await SendCallbackQueryAsync("edit:invalid-guid:s:+5", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.CallbackAnswer, responses[0].Type);
        Assert.Contains("Invalid session ID", responses[0].Text);
    }

    [Fact]
    public async Task CallbackQuery_NonExistentSession_RejectsWithError()
    {
        // Arrange
        const long userId = 20010;
        await RegisterTestUserAsync(telegramUserId: userId);
        var nonExistentId = Guid.NewGuid();

        // Act
        var responses = await SendCallbackQueryAsync($"edit:{nonExistentId}:s:+5", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.CallbackAnswer, responses[0].Type);
        Assert.Contains("Session not found", responses[0].Text);
    }

    [Fact]
    public async Task CallbackQuery_OtherUsersSession_RejectsWithError()
    {
        // Arrange - create a session for user A
        const long userA = 20011;
        const long userB = 20012;

        await RegisterTestUserAsync(telegramUserId: userA);
        await RegisterTestUserAsync(telegramUserId: userB);

        await SendTextAsync("/work", userId: userA);
        await SendTextAsync("/work", userId: userA);

        // Get user A's session ID
        using var scope = Fixture.ServiceProvider.CreateScope();
        var trackingSessionRepository = scope.ServiceProvider.GetRequiredService<ITrackingSessionRepository>();
        var sessions = await trackingSessionRepository.GetRecentSessionsAsync(userA, 1);
        var sessionId = sessions[0].Id;

        // Act - user B tries to edit user A's session
        var responses = await SendCallbackQueryAsync($"edit:{sessionId}:s:+5", userId: userB);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.CallbackAnswer, responses[0].Type);
        Assert.Contains("can only edit your own sessions", responses[0].Text);
    }

    [Fact]
    public async Task EditCommand_ActiveSession_ShowsOngoingStatus()
    {
        // Arrange - create an active (ongoing) session
        const long userId = 20013;
        await RegisterTestUserAsync(telegramUserId: userId);
        await SendTextAsync("/work", userId: userId); // Start but don't stop
        Fixture.MockBotClient.ClearResponses();

        // Act
        var responses = await SendTextAsync("/edit", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Contains("Ended: ongoing", responses[0].Text);
        Assert.Contains("Duration: ongoing", responses[0].Text);
    }

    [Fact]
    public async Task EditCommand_CommuteSession_ShowsDirection()
    {
        // Arrange - create a commute session
        const long userId = 20014;
        await RegisterTestUserAsync(telegramUserId: userId);
        await SendTextAsync("/commute", userId: userId);
        await SendTextAsync("/commute", userId: userId);
        Fixture.MockBotClient.ClearResponses();

        // Act
        var responses = await SendTextAsync("/edit", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Contains("Type: Commute", responses[0].Text);
    }

    [Fact]
    public async Task EditCommand_WithId2_EditsSecondEntry()
    {
        // Arrange - create three tracking sessions today
        const long userId = 20015;
        await RegisterTestUserAsync(telegramUserId: userId);

        // Create three sessions
        await SendTextAsync("/commute", userId: userId);
        await Task.Delay(10);
        await SendTextAsync("/commute", userId: userId);

        await SendTextAsync("/work", userId: userId);
        await Task.Delay(10);
        await SendTextAsync("/work", userId: userId);

        await SendTextAsync("/lunch", userId: userId);
        await Task.Delay(10);
        await SendTextAsync("/lunch", userId: userId);

        Fixture.MockBotClient.ClearResponses();

        // Act - edit entry 2 (second entry from today's list)
        var responses = await SendTextAsync("/edit 2", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("Entry #2:", responses[0].Text);
        Assert.Contains("Work session", responses[0].Text); // Should be the work session (second chronologically)
    }

    [Fact]
    public async Task EditCommand_WithIdZero_ShowsError()
    {
        // Arrange
        const long userId = 20016;
        await RegisterTestUserAsync(telegramUserId: userId);
        await SendTextAsync("/work", userId: userId);
        await SendTextAsync("/work", userId: userId);
        Fixture.MockBotClient.ClearResponses();

        // Act - try to edit entry 0 (invalid)
        var responses = await SendTextAsync("/edit 0", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("Invalid entry ID", responses[0].Text);
        Assert.Contains("positive number", responses[0].Text);
    }

    [Fact]
    public async Task EditCommand_WithNegativeId_ShowsError()
    {
        // Arrange
        const long userId = 20017;
        await RegisterTestUserAsync(telegramUserId: userId);
        await SendTextAsync("/work", userId: userId);
        await SendTextAsync("/work", userId: userId);
        Fixture.MockBotClient.ClearResponses();

        // Act - try to edit entry -1 (invalid)
        var responses = await SendTextAsync("/edit -1", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("Invalid entry ID", responses[0].Text);
        Assert.Contains("positive number", responses[0].Text);
    }

    [Fact]
    public async Task EditCommand_WithInvalidText_ShowsError()
    {
        // Arrange
        const long userId = 20018;
        await RegisterTestUserAsync(telegramUserId: userId);
        await SendTextAsync("/work", userId: userId);
        await SendTextAsync("/work", userId: userId);
        Fixture.MockBotClient.ClearResponses();

        // Act - try to edit with invalid text
        var responses = await SendTextAsync("/edit abc", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("Invalid entry ID", responses[0].Text);
        Assert.Contains("positive number", responses[0].Text);
    }

    [Fact]
    public async Task EditCommand_WithIdNoEntriesToday_ShowsError()
    {
        // Arrange - registered but no entries
        const long userId = 20019;
        await RegisterTestUserAsync(telegramUserId: userId);

        // Act - try to edit entry 1 when there are no entries today
        var responses = await SendTextAsync("/edit 1", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("don't have any tracking entries for today", responses[0].Text);
    }
}
