using Telegram.Bot.Requests;
using TimeSheet.Tests.Integration.Base;
using TimeSheet.Tests.Integration.Fixtures;
using TimeSheet.Tests.Integration.Mocks;

namespace TimeSheet.Tests.Integration.TelegramTests;

/// <summary>
/// Integration tests for the Telegram UpdateHandler.
/// Tests the full system from Telegram layer through to persistence.
/// </summary>
public class UpdateHandlerTests(TelegramBotTestFixture fixture) : TelegramBotTestBase(fixture)
{
    [Fact]
    public async Task HandleTextMessage_LogsIncomingMessage()
    {
        // Arrange
        const string testMessage = "/work";

        // Act
        var responses = await SendTextAsync(testMessage);

        // Assert
        // Command processing is now implemented - verify response is sent
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("Started working", responses[0].Text);
    }

    [Fact]
    public async Task HandleTextMessage_WithDifferentUser_LogsCorrectly()
    {
        // Arrange
        const string testMessage = "Hello bot";
        const long customChatId = 99999;
        const long customUserId = 11111;
        const string customUsername = "customuser";

        // Act
        var responses = await SendTextAsync(
            testMessage,
            chatId: customChatId,
            userId: customUserId,
            username: customUsername);

        // Assert
        // No responses expected yet (Epic 2 will implement command processing)
        AssertNoResponse(responses);
    }

    [Fact]
    public async Task HandleCallbackQuery_ProcessesCorrectly()
    {
        // Arrange
        const string callbackData = "edit_-5m";

        // Act
        var responses = await SendCallbackQueryAsync(callbackData);

        // Assert
        // No responses expected yet (callback handling will be in Epic 4)
        AssertNoResponse(responses);
    }

    [Fact]
    public async Task MockBotClient_CapturesMultipleResponses()
    {
        // This test demonstrates the response capture mechanism
        // It will be useful once UpdateHandler actually sends responses

        // Arrange - manually send a response through the mock client using extension method
        var request = new SendMessageRequest
        {
            ChatId = 12345,
            Text = "Test response"
        };

        // Call SendRequest directly (the core method in Telegram.Bot 22.x)
        await Fixture.MockBotClient.Client.SendRequest(request, CancellationToken.None);

        // Assert
        var responses = Fixture.MockBotClient.Responses;
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Equal("Test response", responses[0].Text);
    }
}
