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
        // New flat format: label appears without numbering
        Assert.Contains("Work", text);
        // Should contain a time range arrow
        Assert.Contains("→", text);
        // Should contain totals section
        Assert.Contains("Work total", text);
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

        // Check that all entry types are shown
        Assert.Contains("Commute to work", text);
        Assert.Contains("Work", text);
        Assert.Contains("Lunch", text);

        // Verify chronological order by checking character positions
        var commuteIdx = text.IndexOf("Commute to work", StringComparison.Ordinal);
        var lunchIdx = text.IndexOf("Lunch", StringComparison.Ordinal);

        Assert.True(commuteIdx < lunchIdx, "Commute should appear before Lunch");

        // Verify totals section appears after separator
        var separatorIdx = text.IndexOf("────", StringComparison.Ordinal);
        var workTotalIdx = text.IndexOf("Work total", StringComparison.Ordinal);
        Assert.True(separatorIdx >= 0, "Separator should be present");
        Assert.True(workTotalIdx > separatorIdx, "Work total should appear after separator");
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
        Assert.Contains("Work", text);
        // Ongoing session shows "..." as end time
        Assert.Contains("→ ...", text);
        Assert.Contains("ongoing", text);
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
        Assert.Contains("Commute to work", text);
        Assert.Contains("Work", text);
        Assert.Contains("Commute to home", text);
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

        // Time format in new layout: "HH:mm → HH:mm"
        Assert.Matches(@"\d{2}:\d{2} → \d{2}:\d{2}", text);
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

        // Duration should be in format like "0m" or "1h 30m" (never decimal)
        Assert.Matches(@"\d+[hm]", text);
        // Must NOT contain decimal hours
        Assert.DoesNotMatch(@"\d+\.\d+h", text);
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

        // Should show completed work session (with a real end time, not "...")
        Assert.Contains("Work", text);
        // Completed work session has a time range like "HH:mm → HH:mm"
        Assert.Matches(@"\d{2}:\d{2} → \d{2}:\d{2}", text);

        // Should show ongoing lunch session
        Assert.Contains("Lunch", text);
        // Ongoing session shows "..."
        Assert.Contains("→ ...", text);
    }
}
