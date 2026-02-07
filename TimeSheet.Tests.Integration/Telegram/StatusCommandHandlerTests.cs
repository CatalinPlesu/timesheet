using TimeSheet.Tests.Integration.Base;
using TimeSheet.Tests.Integration.Fixtures;
using TimeSheet.Tests.Integration.Mocks;

namespace TimeSheet.Tests.Integration.TelegramTests;

/// <summary>
/// Integration tests for the /status command handler.
/// Tests displaying current tracking status.
/// </summary>
public class StatusCommandHandlerTests(TelegramBotTestFixture fixture) : TelegramBotTestBase(fixture)
{
    [Fact]
    public async Task Status_WhenIdle_ShouldShowIdleStatus()
    {
        // Arrange
        const long userId = 40001;
        await RegisterTestUserAsync(telegramUserId: userId);

        // Act
        var responses = await SendTextAsync("/status", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("Current Status", responses[0].Text);
        Assert.Contains("Idle", responses[0].Text);
        Assert.Contains("not tracking", responses[0].Text);
    }

    [Fact]
    public async Task Status_WhenWorking_ShouldShowWorkingStatus()
    {
        // Arrange
        const long userId = 40002;
        await RegisterTestUserAsync(telegramUserId: userId);

        // Start working
        await SendTextAsync("/work", userId: userId);
        Fixture.MockBotClient.ClearResponses();

        // Act
        var responses = await SendTextAsync("/status", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        var text = responses[0].Text;
        Assert.Contains("Current Status", text);
        Assert.Contains("Working", text);
        Assert.Contains("Started:", text);
        Assert.Contains("Duration:", text);
        Assert.NotNull(responses[0].ReplyMarkup);
    }

    [Fact]
    public async Task Status_WithTargetWorkHours_ShouldShowProgress()
    {
        // Arrange
        const long userId = 40003;
        await RegisterTestUserAsync(telegramUserId: userId);

        // Set target work hours
        await SendTextAsync("/settings target 8", userId: userId);
        Fixture.MockBotClient.ClearResponses();

        // Act
        var responses = await SendTextAsync("/status", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        var text = responses[0].Text;
        Assert.Contains("Target:", text);
        Assert.Contains("8h", text);
        Assert.Contains("complete", text);
    }

    [Fact]
    public async Task Status_Alias_ShouldWork()
    {
        // Arrange
        const long userId = 40004;
        await RegisterTestUserAsync(telegramUserId: userId);

        // Act
        var responses = await SendTextAsync("/s", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("Current Status", responses[0].Text);
    }
}
