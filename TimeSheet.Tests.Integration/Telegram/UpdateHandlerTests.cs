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
    public async Task CommuteCommand_Started_IncludesActionButtons()
    {
        // Arrange - use unique user ID to avoid state pollution from other tests
        const long userId = 10005;
        await RegisterTestUserAsync(telegramUserId: userId);

        // Act
        var responses = await SendTextAsync("/commute", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Contains("Started tracking commuting", responses[0].Text);
        // Should include inline keyboard with action buttons instead of text suggestions
        Assert.NotNull(responses[0].ReplyMarkup);
    }

    [Fact]
    public async Task WorkCommand_Started_IncludesActionButtons()
    {
        // Arrange - use unique user ID to avoid state pollution from other tests
        const long userId = 10006;
        await RegisterTestUserAsync(telegramUserId: userId);

        // Act
        var responses = await SendTextAsync("/work", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Contains("Started tracking working", responses[0].Text);
        // Should include inline keyboard with action buttons instead of text suggestions
        Assert.NotNull(responses[0].ReplyMarkup);
    }

    [Fact]
    public async Task LunchCommand_Started_IncludesActionButtons()
    {
        // Arrange - use unique user ID to avoid state pollution from other tests
        const long userId = 10007;
        await RegisterTestUserAsync(telegramUserId: userId);

        // Act
        var responses = await SendTextAsync("/lunch", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Contains("Started tracking on lunch break", responses[0].Text);
        // Should include inline keyboard with action buttons instead of text suggestions
        Assert.NotNull(responses[0].ReplyMarkup);
    }

    [Fact]
    public async Task CommuteCommand_Ended_IncludesActionButtons()
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
        // Should include inline keyboard with action buttons instead of text suggestions
        Assert.NotNull(responses[0].ReplyMarkup);
    }

    [Fact]
    public async Task WorkCommand_Ended_IncludesActionButtons()
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
        // Should include inline keyboard with action buttons instead of text suggestions
        Assert.NotNull(responses[0].ReplyMarkup);
    }

    [Fact]
    public async Task SwitchingStates_IncludesActionButtons()
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
        // Should include inline keyboard with action buttons instead of text suggestions
        Assert.NotNull(responses[0].ReplyMarkup);
    }

    [Fact]
    public async Task CommandAlias_SingleLevel_WorksCorrectly()
    {
        // Arrange
        const long userId = 20099; // Unique ID to avoid state pollution from other test files
        await RegisterTestUserAsync(telegramUserId: userId);

        // Act - use aliases for work command
        var responses1 = await SendTextAsync("/w", userId: userId);

        // Assert first response immediately (before it gets cleared)
        Assert.Single(responses1);
        Assert.Contains("Started tracking working", responses1[0].Text);

        // Act - toggle to stop
        var responses2 = await SendTextAsync("/w", userId: userId);

        // Assert second response
        Assert.Single(responses2);
        Assert.Contains("Stopped work session", responses2[0].Text);
    }

    [Fact]
    public async Task CommandAlias_MultiLevel_HelpReport_WorksCorrectly()
    {
        // Arrange
        const long userId = 20002;
        await RegisterTestUserAsync(telegramUserId: userId);

        // Act - use multi-level alias: /h r should expand to /help report
        var responses = await SendTextAsync("/h r", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("Report Commands", responses[0].Text);
        Assert.Contains("/report day", responses[0].Text);
    }

    [Fact]
    public async Task CommandAlias_MultiLevel_HelpTracking_WorksCorrectly()
    {
        // Arrange
        const long userId = 20003;
        await RegisterTestUserAsync(telegramUserId: userId);

        // Act - use multi-level alias: /h t should expand to /help tracking
        var responses = await SendTextAsync("/h t", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("Tracking Commands", responses[0].Text);
        Assert.Contains("/commute", responses[0].Text);
    }

    [Fact]
    public async Task CommandAlias_MultiLevel_HelpSettings_WorksCorrectly()
    {
        // Arrange
        const long userId = 20004;
        await RegisterTestUserAsync(telegramUserId: userId);

        // Act - use multi-level alias: /h s should expand to /help settings
        var responses = await SendTextAsync("/h s", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("Settings Usage", responses[0].Text);
        Assert.Contains("Timezone", responses[0].Text);
    }

    [Fact]
    public async Task CommandAlias_MultiLevel_ReportDay_WorksCorrectly()
    {
        // Arrange
        const long userId = 20005;
        await RegisterTestUserAsync(telegramUserId: userId);

        // Act - use multi-level alias: /r d should expand to /report day
        var responses = await SendTextAsync("/r d", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("Report: Today", responses[0].Text);
    }

    [Fact]
    public async Task CommandAlias_MultiLevel_ReportWeek_WorksCorrectly()
    {
        // Arrange
        const long userId = 20006;
        await RegisterTestUserAsync(telegramUserId: userId);

        // Act - use multi-level alias: /r w should expand to /report week
        var responses = await SendTextAsync("/r w", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("Report: This Week", responses[0].Text);
    }

    [Fact]
    public async Task CommandAlias_MultiLevel_SettingsUtc_WorksCorrectly()
    {
        // Arrange
        const long userId = 20007;
        await RegisterTestUserAsync(telegramUserId: userId);

        // Act - use multi-level alias: /se u +2 should expand to /settings utc +2
        var responses = await SendTextAsync("/se u +2", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("Timezone updated to UTC", responses[0].Text);
    }

    [Fact]
    public async Task CommandAlias_AllTrackingCommands_WorkCorrectly()
    {
        // Arrange
        const long userId = 20008;
        await RegisterTestUserAsync(telegramUserId: userId);

        // Act & Assert - /c for commute
        var commuteResponses = await SendTextAsync("/c", userId: userId);
        Assert.Contains("Started tracking commuting", commuteResponses[0].Text);
        Fixture.MockBotClient.ClearResponses();

        // Act & Assert - /w for work
        var workResponses = await SendTextAsync("/w", userId: userId);
        Assert.Contains("started tracking working", workResponses[0].Text);
        Fixture.MockBotClient.ClearResponses();

        // Act & Assert - /l for lunch
        var lunchResponses = await SendTextAsync("/l", userId: userId);
        Assert.Contains("started tracking on lunch break", lunchResponses[0].Text);
        Fixture.MockBotClient.ClearResponses();

        // Act & Assert - /s for status
        var statusResponses = await SendTextAsync("/s", userId: userId);
        Assert.Contains("Current Status", statusResponses[0].Text);
    }

    [Fact]
    public async Task HelpCommand_RegularUser_DoesNotShowAdminCommands()
    {
        // Arrange - register a regular (non-admin) user
        const long userId = 30001;
        await RegisterTestUserAsync(telegramUserId: userId, isAdmin: false);

        // Act - request main help
        var responses = await SendTextAsync("/help", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("TimeSheet Bot", responses[0].Text);
        // Should NOT contain admin section
        Assert.DoesNotContain("Admin:", responses[0].Text);
        Assert.DoesNotContain("/g", responses[0].Text);
        // Should not show /help admin option
        Assert.DoesNotContain("/help admin", responses[0].Text);
    }

    [Fact]
    public async Task HelpCommand_AdminUser_ShowsAdminCommands()
    {
        // Arrange - register an admin user
        const long userId = 30002;
        await RegisterTestUserAsync(telegramUserId: userId, isAdmin: true);

        // Act - request main help
        var responses = await SendTextAsync("/help", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("TimeSheet Bot", responses[0].Text);
        // Should contain admin section
        Assert.Contains("Admin:", responses[0].Text);
        Assert.Contains("/g", responses[0].Text);
        // Should show /help admin option
        Assert.Contains("/help admin", responses[0].Text);
    }

    [Fact]
    public async Task HelpCommand_AdminSubmenu_AdminUser_ShowsAdminHelp()
    {
        // Arrange - register an admin user
        const long userId = 30003;
        await RegisterTestUserAsync(telegramUserId: userId, isAdmin: true);

        // Act - request admin help submenu
        var responses = await SendTextAsync("/help admin", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("Admin Commands", responses[0].Text);
        Assert.Contains("/generate", responses[0].Text);
        Assert.Contains("BIP39 mnemonic", responses[0].Text);
    }

    [Fact]
    public async Task HelpCommand_AdminSubmenu_RegularUser_ShowsAccessDenied()
    {
        // Arrange - register a regular (non-admin) user
        const long userId = 30004;
        await RegisterTestUserAsync(telegramUserId: userId, isAdmin: false);

        // Act - attempt to request admin help submenu
        var responses = await SendTextAsync("/help admin", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("Admin commands are only available to administrators", responses[0].Text);
    }

    [Fact]
    public async Task HelpCommand_Alias_WorksCorrectly()
    {
        // Arrange
        const long userId = 30005;
        await RegisterTestUserAsync(telegramUserId: userId);

        // Act - use alias /h instead of /help
        var responses = await SendTextAsync("/h", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("TimeSheet Bot", responses[0].Text);
    }

    [Fact]
    public async Task HelpCommand_AdminAlias_WorksCorrectly()
    {
        // Arrange - register an admin user
        const long userId = 30006;
        await RegisterTestUserAsync(telegramUserId: userId, isAdmin: true);

        // Act - use multi-level alias /h admin (expanded from potential future /h a)
        var responses = await SendTextAsync("/h admin", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("Admin Commands", responses[0].Text);
    }
}
