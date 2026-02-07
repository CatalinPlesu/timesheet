using TimeSheet.Tests.Integration.Base;
using TimeSheet.Tests.Integration.Fixtures;
using TimeSheet.Tests.Integration.Mocks;

namespace TimeSheet.Tests.Integration.TelegramTests;

/// <summary>
/// Integration tests for the /list command handler.
/// Tests displaying today's time entries.
/// </summary>
public class ListCommandHandlerTests(TelegramBotTestFixture fixture) : TelegramBotTestBase(fixture)
{
    [Fact]
    public async Task ListCommand_NoEntries_ShowsEmptyMessage()
    {
        // Arrange
        const long userId = 30001;
        await RegisterTestUserAsync(telegramUserId: userId);

        // Act
        var responses = await SendTextAsync("/list", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);
        Assert.Contains("No tracking entries for today", responses[0].Text);
    }

    [Fact]
    public async Task ListCommand_WithOneEntry_ShowsEntry()
    {
        // Arrange
        const long userId = 30002;
        await RegisterTestUserAsync(telegramUserId: userId);

        // Create a work session
        await SendTextAsync("/work", userId: userId);
        await Task.Delay(10); // Small delay
        await SendTextAsync("/work", userId: userId); // Stop it

        Fixture.MockBotClient.ClearResponses();

        // Act
        var responses = await SendTextAsync("/list", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);

        var text = responses[0].Text;
        Assert.Contains("Today's entries", text);
        Assert.Contains("1. Work session", text);
        Assert.Contains("Started:", text);
        Assert.Contains("Ended:", text);
        Assert.Contains("Duration:", text);
    }

    [Fact]
    public async Task ListCommand_WithMultipleEntries_ShowsAllInChronologicalOrder()
    {
        // Arrange
        const long userId = 30003;
        await RegisterTestUserAsync(telegramUserId: userId);

        // Create multiple sessions
        await SendTextAsync("/commute", userId: userId);
        await Task.Delay(10);
        await SendTextAsync("/work", userId: userId); // Ends commute, starts work
        await Task.Delay(10);
        await SendTextAsync("/lunch", userId: userId); // Ends work, starts lunch
        await Task.Delay(10);
        await SendTextAsync("/work", userId: userId); // Ends lunch, starts work
        await Task.Delay(10);
        await SendTextAsync("/work", userId: userId); // Ends work

        Fixture.MockBotClient.ClearResponses();

        // Act
        var responses = await SendTextAsync("/list", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);

        var text = responses[0].Text ?? string.Empty;
        Assert.Contains("Today's entries", text);

        // Check that all entries are shown in order
        // Entry numbers should appear sequentially
        Assert.Contains("1. Commute to work", text);
        Assert.Contains("2. Work session", text);
        Assert.Contains("3. Lunch break", text);
        Assert.Contains("4. Work session", text);

        // Verify order by checking indices
        var commute1Idx = text.IndexOf("1. Commute to work", StringComparison.Ordinal);
        var work2Idx = text.IndexOf("2. Work session", StringComparison.Ordinal);
        var lunch3Idx = text.IndexOf("3. Lunch break", StringComparison.Ordinal);
        var work4Idx = text.IndexOf("4. Work session", StringComparison.Ordinal);

        Assert.True(commute1Idx < work2Idx, "First entry should appear before second");
        Assert.True(work2Idx < lunch3Idx, "Second entry should appear before third");
        Assert.True(lunch3Idx < work4Idx, "Third entry should appear before fourth");
    }

    [Fact]
    public async Task ListCommand_WithOngoingSession_ShowsOngoingStatus()
    {
        // Arrange
        const long userId = 30004;
        await RegisterTestUserAsync(telegramUserId: userId);

        // Create an ongoing work session
        await SendTextAsync("/work", userId: userId);

        Fixture.MockBotClient.ClearResponses();

        // Act
        var responses = await SendTextAsync("/list", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);

        var text = responses[0].Text;
        Assert.Contains("Today's entries", text);
        Assert.Contains("1. Work session", text);
        Assert.Contains("Started:", text);
        Assert.Contains("Ended: ongoing", text);
        Assert.Contains("Duration: ongoing", text);
    }

    [Fact]
    public async Task ListCommand_WithCommuteToHome_ShowsCorrectDirection()
    {
        // Arrange
        const long userId = 30005;
        await RegisterTestUserAsync(telegramUserId: userId);

        // Create commute to work, then work, then commute to home
        await SendTextAsync("/commute", userId: userId);
        await Task.Delay(10);
        await SendTextAsync("/work", userId: userId);
        await Task.Delay(10);
        await SendTextAsync("/commute", userId: userId); // This should be to home
        await Task.Delay(10);
        await SendTextAsync("/commute", userId: userId); // Stop commute

        Fixture.MockBotClient.ClearResponses();

        // Act
        var responses = await SendTextAsync("/list", userId: userId);

        // Assert
        Assert.Single(responses);
        Assert.Equal(ResponseType.Message, responses[0].Type);

        var text = responses[0].Text;
        Assert.Contains("1. Commute to work", text);
        Assert.Contains("2. Work session", text);
        Assert.Contains("3. Commute to home", text);
    }

    [Fact]
    public async Task ListCommand_ShowsTimeInHHMMFormat()
    {
        // Arrange
        const long userId = 30006;
        await RegisterTestUserAsync(telegramUserId: userId);

        // Create a session
        await SendTextAsync("/work", userId: userId);
        await Task.Delay(10);
        await SendTextAsync("/work", userId: userId);

        Fixture.MockBotClient.ClearResponses();

        // Act
        var responses = await SendTextAsync("/list", userId: userId);

        // Assert
        Assert.Single(responses);
        var text = responses[0].Text;

        // Time format should be HH:MM (e.g., "09:30", "14:45")
        // Use regex to verify format
        Assert.Matches(@"Started: \d{2}:\d{2}", text);
        Assert.Matches(@"Ended: \d{2}:\d{2}", text);
    }

    [Fact]
    public async Task ListCommand_ShowsDurationInCorrectFormat()
    {
        // Arrange
        const long userId = 30007;
        await RegisterTestUserAsync(telegramUserId: userId);

        // Create a session
        await SendTextAsync("/work", userId: userId);
        await Task.Delay(100); // Ensure some duration
        await SendTextAsync("/work", userId: userId);

        Fixture.MockBotClient.ClearResponses();

        // Act
        var responses = await SendTextAsync("/list", userId: userId);

        // Assert
        Assert.Single(responses);
        var text = responses[0].Text;

        // Duration should be in format like "0m" or "1h 30m"
        Assert.Matches(@"Duration: \d+[hm]", text);
    }

    [Fact]
    public async Task ListCommand_NonRegisteredUser_NoResponse()
    {
        // Arrange - no user registered
        const long userId = 30008;

        // Act
        var responses = await SendTextAsync("/list", userId: userId);

        // Assert - bot should silently ignore non-registered users
        Assert.Empty(responses);
    }

    [Fact]
    public async Task ListCommand_ShowsDateInISO8601Format()
    {
        // Arrange
        const long userId = 30009;
        await RegisterTestUserAsync(telegramUserId: userId);

        // Create a session
        await SendTextAsync("/work", userId: userId);
        await SendTextAsync("/work", userId: userId);

        Fixture.MockBotClient.ClearResponses();

        // Act
        var responses = await SendTextAsync("/list", userId: userId);

        // Assert
        Assert.Single(responses);
        var text = responses[0].Text;

        // Date format should be yyyy-MM-dd
        Assert.Matches(@"Today's entries \(\d{4}-\d{2}-\d{2}\)", text);
    }

    [Fact]
    public async Task ListCommand_WithMixedCompletedAndOngoingSessions_ShowsBoth()
    {
        // Arrange
        const long userId = 30010;
        await RegisterTestUserAsync(telegramUserId: userId);

        // Create completed session
        await SendTextAsync("/work", userId: userId);
        await Task.Delay(10);
        await SendTextAsync("/lunch", userId: userId); // Ends work, starts lunch

        Fixture.MockBotClient.ClearResponses();

        // Act
        var responses = await SendTextAsync("/list", userId: userId);

        // Assert
        Assert.Single(responses);
        var text = responses[0].Text ?? string.Empty;

        // Should show completed work session
        Assert.Contains("1. Work session", text);
        // Check that work session has an actual end time (not "ongoing")
        var workSessionIdx = text.IndexOf("1. Work session", StringComparison.Ordinal);
        var lunchBreakIdx = text.IndexOf("2. Lunch break", StringComparison.Ordinal);
        var workSection = text[workSessionIdx..lunchBreakIdx];
        Assert.DoesNotContain("Ended: ongoing", workSection);

        // Should show ongoing lunch session
        Assert.Contains("2. Lunch break", text);
        var lunchSection = text[lunchBreakIdx..];
        Assert.Contains("Ended: ongoing", lunchSection);
    }
}
