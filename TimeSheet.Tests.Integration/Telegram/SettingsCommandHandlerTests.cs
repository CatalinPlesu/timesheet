using TimeSheet.Tests.Integration.Base;
using TimeSheet.Tests.Integration.Fixtures;
using TimeSheet.Tests.Integration.Mocks;

namespace TimeSheet.Tests.Integration.TelegramTests;

/// <summary>
/// Integration tests for the /settings command handler.
/// </summary>
public class SettingsCommandHandlerTests(TelegramBotTestFixture fixture) : TelegramBotTestBase(fixture)
{
    [Fact]
    public async Task HandleSettings_RegisteredUser_ShowsCurrentSettings()
    {
        // Arrange
        const long userId = 20001;
        await RegisterTestUserAsync(telegramUserId: userId, utcOffsetMinutes: 120); // UTC+2

        // Act
        var responses = await SendTextAsync("/settings", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("Settings", responses[0].Text);
        Assert.Contains("UTC+2", responses[0].Text);
    }

    [Fact]
    public async Task HandleSettings_NegativeOffset_DisplaysCorrectly()
    {
        // Arrange
        const long userId = 20002;
        await RegisterTestUserAsync(telegramUserId: userId, utcOffsetMinutes: -300); // UTC-5

        // Act
        var responses = await SendTextAsync("/settings", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("UTC-5", responses[0].Text);
    }

    [Fact]
    public async Task HandleSettings_ZeroOffset_DisplaysCorrectly()
    {
        // Arrange
        const long userId = 20003;
        await RegisterTestUserAsync(telegramUserId: userId, utcOffsetMinutes: 0);

        // Act
        var responses = await SendTextAsync("/settings", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("UTC+0", responses[0].Text);
    }

    [Fact]
    public async Task HandleSettingsUtc_ValidOffset_UpdatesSuccessfully()
    {
        // Arrange
        const long userId = 20004;
        await RegisterTestUserAsync(telegramUserId: userId, utcOffsetMinutes: 0);

        // Act
        var responses = await SendTextAsync("/settings utc +3", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("Timezone updated to UTC+3", responses[0].Text);

        // Verify the update persisted
        Fixture.MockBotClient.ClearResponses();
        var verifyResponses = await SendTextAsync("/settings", userId: userId);
        Assert.Contains("UTC+3", verifyResponses[0].Text);
    }

    [Fact]
    public async Task HandleSettingsUtc_NegativeOffset_UpdatesSuccessfully()
    {
        // Arrange
        const long userId = 20005;
        await RegisterTestUserAsync(telegramUserId: userId, utcOffsetMinutes: 0);

        // Act
        var responses = await SendTextAsync("/settings utc -7", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("Timezone updated to UTC-7", responses[0].Text);

        // Verify the update persisted
        Fixture.MockBotClient.ClearResponses();
        var verifyResponses = await SendTextAsync("/settings", userId: userId);
        Assert.Contains("UTC-7", verifyResponses[0].Text);
    }

    [Fact]
    public async Task HandleSettingsUtc_InvalidFormat_ShowsError()
    {
        // Arrange
        const long userId = 20006;
        await RegisterTestUserAsync(telegramUserId: userId, utcOffsetMinutes: 0);

        // Act
        var responses = await SendTextAsync("/settings utc abc", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("Invalid offset", responses[0].Text);
    }

    [Fact]
    public async Task HandleSettingsUtc_OutOfRange_ShowsError()
    {
        // Arrange
        const long userId = 20007;
        await RegisterTestUserAsync(telegramUserId: userId, utcOffsetMinutes: 0);

        // Act - try to set UTC+20 (out of range)
        var responses = await SendTextAsync("/settings utc 20", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("must be between -12 and +14", responses[0].Text);
    }

    [Fact]
    public async Task HandleSettingsUtc_BelowMinimum_ShowsError()
    {
        // Arrange
        const long userId = 20008;
        await RegisterTestUserAsync(telegramUserId: userId, utcOffsetMinutes: 0);

        // Act - try to set UTC-15 (out of range)
        var responses = await SendTextAsync("/settings utc -15", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("must be between -12 and +14", responses[0].Text);
    }

    [Fact]
    public async Task HandleSettingsUtc_MissingOffset_ShowsUsage()
    {
        // Arrange
        const long userId = 20009;
        await RegisterTestUserAsync(telegramUserId: userId, utcOffsetMinutes: 0);

        // Act
        var responses = await SendTextAsync("/settings utc", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("Usage", responses[0].Text);
    }

    [Fact]
    public async Task HandleSettingsUtc_EdgeCases_AllValid()
    {
        // Test minimum valid offset (UTC-12)
        const long userId1 = 20010;
        await RegisterTestUserAsync(telegramUserId: userId1, utcOffsetMinutes: 0);
        var responses1 = await SendTextAsync("/settings utc -12", userId: userId1);
        Assert.Contains("UTC-12", responses1[0].Text);

        // Test maximum valid offset (UTC+14)
        const long userId2 = 20011;
        await RegisterTestUserAsync(telegramUserId: userId2, utcOffsetMinutes: 0);
        var responses2 = await SendTextAsync("/settings utc 14", userId: userId2);
        Assert.Contains("UTC+14", responses2[0].Text);
    }

    [Fact]
    public async Task HandleSettings_NonRegisteredUser_Ignored()
    {
        // Arrange - no registration
        const long userId = 20012;

        // Act
        var responses = await SendTextAsync("/settings", userId: userId);

        // Assert - should be ignored (no response)
        AssertNoResponse(responses);
    }
}
