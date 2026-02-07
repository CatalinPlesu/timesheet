using Microsoft.Extensions.DependencyInjection;
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
        await RegisterTestUserAsync(); // Register test user to pass auth middleware

        // Act
        var responses = await SendTextAsync(testMessage);

        // Assert
        // Command processing is now implemented - verify response is sent
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("Started tracking working", responses[0].Text);
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
    public async Task HandleCallbackQuery_NonRegisteredUser_RejectsWithAlert()
    {
        // Arrange
        const string callbackData = "edit:some-session-id:+5";

        // Act
        var responses = await SendCallbackQueryAsync(callbackData);

        // Assert
        // Should get a callback answer telling them to register
        Assert.Single(responses);
        Assert.Equal(ResponseType.CallbackAnswer, responses[0].Type);
        Assert.Contains("need to register", responses[0].Text);
        Assert.True(responses[0].ShowAlert);
    }

    [Fact]
    public async Task MockBotClient_CapturesMultipleResponses()
    {
        // This test demonstrates the response capture mechanism
        // It will be useful once UpdateHandler actually sends responses

        // Arrange - clear any previous responses and manually send a response
        Fixture.MockBotClient.ClearResponses();

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

    [Fact]
    public async Task TrackingCommand_StartingState_ShowsStartedTrackingMessage()
    {
        // Arrange - use unique user ID to avoid state pollution from other tests
        const long userId = 10001;
        await RegisterTestUserAsync(telegramUserId: userId);

        // Act
        var responses = await SendTextAsync("/commute", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("Started tracking commuting", responses[0].Text);
    }

    [Fact]
    public async Task TrackingCommand_ToggleStop_ShowsStoppedWithDuration()
    {
        // Arrange - use unique user ID to avoid state pollution from other tests
        const long userId = 10002;
        await RegisterTestUserAsync(telegramUserId: userId);
        await SendTextAsync("/work", userId: userId);
        Fixture.MockBotClient.ClearResponses();

        // Act - toggle the same command to stop it
        var responses = await SendTextAsync("/work", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.StartsWith("Stopped work session, duration:", responses[0].Text);
    }

    [Fact]
    public async Task TrackingCommand_SwitchingStates_ShowsStoppedAndStartedMessage()
    {
        // Arrange - use unique user ID to avoid state pollution from other tests
        const long userId = 10003;
        await RegisterTestUserAsync(telegramUserId: userId);
        await SendTextAsync("/commute", userId: userId);
        Fixture.MockBotClient.ClearResponses();

        // Act - switch to a different state
        var responses = await SendTextAsync("/work", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("Stopped commuting", responses[0].Text);
        Assert.Contains("started tracking working", responses[0].Text);
    }

    [Fact]
    public async Task TrackingCommand_AllStates_UseCorrectStateName()
    {
        // Arrange - use unique user ID to avoid state pollution from other tests
        const long userId = 10004;
        await RegisterTestUserAsync(telegramUserId: userId);

        // Act & Assert - Test commute
        var commuteResponses = await SendTextAsync("/commute", userId: userId);
        Assert.Contains("Started tracking commuting", commuteResponses[0].Text);
        Fixture.MockBotClient.ClearResponses();

        // Act & Assert - Test work (should also show stopped commuting)
        var workResponses = await SendTextAsync("/work", userId: userId);
        Assert.Contains("Stopped commuting", workResponses[0].Text);
        Assert.Contains("started tracking working", workResponses[0].Text);
        Fixture.MockBotClient.ClearResponses();

        // Act & Assert - Test lunch (should also show stopped work session)
        var lunchResponses = await SendTextAsync("/lunch", userId: userId);
        Assert.Contains("Stopped work session", lunchResponses[0].Text);
        Assert.Contains("started tracking on lunch break", lunchResponses[0].Text);
    }

    [Fact]
    public async Task CommuteCommand_Started_IncludesSuggestion()
    {
        // Arrange - use unique user ID to avoid state pollution from other tests
        const long userId = 10005;
        await RegisterTestUserAsync(telegramUserId: userId);

        // Act
        var responses = await SendTextAsync("/commute", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Contains("Started tracking commuting", responses[0].Text);
        Assert.Contains("Press /commute to stop when you reach your destination, or /work to start working", responses[0].Text);
    }

    [Fact]
    public async Task WorkCommand_Started_IncludesSuggestion()
    {
        // Arrange - use unique user ID to avoid state pollution from other tests
        const long userId = 10006;
        await RegisterTestUserAsync(telegramUserId: userId);

        // Act
        var responses = await SendTextAsync("/work", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Contains("Started tracking working", responses[0].Text);
        // Should include a suggestion about lunch, work stop, or commute
        Assert.Contains("Press", responses[0].Text);
        Assert.Contains("/lunch", responses[0].Text);
    }

    [Fact]
    public async Task LunchCommand_Started_IncludesSuggestion()
    {
        // Arrange - use unique user ID to avoid state pollution from other tests
        const long userId = 10007;
        await RegisterTestUserAsync(telegramUserId: userId);

        // Act
        var responses = await SendTextAsync("/lunch", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Contains("Started tracking on lunch break", responses[0].Text);
        Assert.Contains("Press /work when you're ready to continue working", responses[0].Text);
    }

    [Fact]
    public async Task CommuteCommand_Ended_IncludesSuggestion()
    {
        // Arrange - use unique user ID to avoid state pollution from other tests
        const long userId = 10008;
        await RegisterTestUserAsync(telegramUserId: userId);
        await SendTextAsync("/commute", userId: userId);
        Fixture.MockBotClient.ClearResponses();

        // Act - toggle to stop commuting
        var responses = await SendTextAsync("/commute", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Contains("Stopped commuting", responses[0].Text);
        Assert.Contains("Press /work to start tracking your work time", responses[0].Text);
    }

    [Fact]
    public async Task WorkCommand_Ended_IncludesSuggestion()
    {
        // Arrange - use unique user ID to avoid state pollution from other tests
        const long userId = 10009;
        await RegisterTestUserAsync(telegramUserId: userId);
        await SendTextAsync("/work", userId: userId);
        Fixture.MockBotClient.ClearResponses();

        // Act - toggle to stop working
        var responses = await SendTextAsync("/work", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Contains("Stopped work session", responses[0].Text);
        Assert.Contains("Press /commute if you're heading home", responses[0].Text);
    }

    [Fact]
    public async Task SwitchingStates_IncludesSuggestion()
    {
        // Arrange - use unique user ID to avoid state pollution from other tests
        const long userId = 10010;
        await RegisterTestUserAsync(telegramUserId: userId);
        await SendTextAsync("/commute", userId: userId);
        Fixture.MockBotClient.ClearResponses();

        // Act - switch to work
        var responses = await SendTextAsync("/work", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Contains("Stopped commuting", responses[0].Text);
        Assert.Contains("started tracking working", responses[0].Text);
        // Should include work-related suggestions
        Assert.Contains("Press", responses[0].Text);
        Assert.Contains("/lunch", responses[0].Text);
    }
}
