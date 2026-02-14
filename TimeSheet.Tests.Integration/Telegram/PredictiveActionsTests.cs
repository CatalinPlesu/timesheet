using Telegram.Bot.Types.ReplyMarkups;
using TimeSheet.Core.Domain.Enums;
using TimeSheet.Tests.Integration.Base;
using TimeSheet.Tests.Integration.Fixtures;
using TimeSheet.Tests.Integration.Mocks;

namespace TimeSheet.Tests.Integration.TelegramTests;

/// <summary>
/// Tests for predictive action buttons on tracking commands.
/// </summary>
public class PredictiveActionsTests : TelegramBotTestBase
{
    public PredictiveActionsTests(TelegramBotTestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task CommuteStart_ShouldShowStopCommuteAndStartWorkButtons()
    {
        // Arrange
        await RegisterTestUserAsync();

        // Act
        var responses = await SendTextAsync("/commute");

        // Assert
        AssertHasResponse(responses);
        var messageResponse = responses.First(r => r.Type == ResponseType.Message);
        Assert.NotNull(messageResponse.ReplyMarkup);
        Assert.IsType<InlineKeyboardMarkup>(messageResponse.ReplyMarkup);

        var keyboard = (InlineKeyboardMarkup)messageResponse.ReplyMarkup;
        var buttons = keyboard.InlineKeyboard.SelectMany(row => row).ToList();

        // Should have 2 buttons: Stop Commute and Start Work
        Assert.Equal(2, buttons.Count);
        Assert.Contains(buttons, b => b.Text == "Stop Commute" && b.CallbackData == "track:commute");
        Assert.Contains(buttons, b => b.Text == "Start Work" && b.CallbackData == "track:work");
    }

    [Fact]
    public async Task WorkStart_DuringLunchTime_ShouldPrioritizeLunchButton()
    {
        // Arrange
        await RegisterTestUserAsync(utcOffsetMinutes: 0); // UTC timezone for predictable testing

        // Set the work start time to 12:00 UTC (lunchtime)
        var lunchTime = DateTime.UtcNow.Date.AddHours(12);

        // Act
        var responses = await SendTextAsync($"/work {lunchTime:HH:mm}");

        // Assert
        AssertHasResponse(responses);
        var messageResponse = responses.First(r => r.Type == ResponseType.Message);
        Assert.NotNull(messageResponse.ReplyMarkup);
        var keyboard = (InlineKeyboardMarkup)messageResponse.ReplyMarkup;
        var firstRowButtons = keyboard.InlineKeyboard.First().ToList();

        // First row should prioritize lunch button during lunchtime
        Assert.Contains(firstRowButtons, b => b.Text == "Take Lunch" && b.CallbackData == "track:lunch");
    }

    [Fact]
    public async Task WorkStart_OutsideLunchTime_ShouldShowStopWorkFirst()
    {
        // Arrange
        await RegisterTestUserAsync(utcOffsetMinutes: 0);

        // Set the work start time to 09:00 UTC (morning, outside lunchtime)
        var morningTime = DateTime.UtcNow.Date.AddHours(9);

        // Act
        var responses = await SendTextAsync($"/work {morningTime:HH:mm}");

        // Assert
        AssertHasResponse(responses);
        var messageResponse = responses.First(r => r.Type == ResponseType.Message);
        Assert.NotNull(messageResponse.ReplyMarkup);
        var keyboard = (InlineKeyboardMarkup)messageResponse.ReplyMarkup;
        var allButtons = keyboard.InlineKeyboard.SelectMany(row => row).ToList();

        // Should have Stop Work, Take Lunch, and Commute Home buttons
        Assert.Contains(allButtons, b => b.Text == "Stop Work");
        Assert.Contains(allButtons, b => b.Text == "Take Lunch");
        Assert.Contains(allButtons, b => b.Text == "Commute Home");
    }

    [Fact]
    public async Task LunchStart_ShouldShowBackToWorkButton()
    {
        // Arrange
        await RegisterTestUserAsync();

        // Act
        var responses = await SendTextAsync("/lunch");

        // Assert
        AssertHasResponse(responses);
        var messageResponse = responses.First(r => r.Type == ResponseType.Message);
        Assert.NotNull(messageResponse.ReplyMarkup);
        var keyboard = (InlineKeyboardMarkup)messageResponse.ReplyMarkup;
        var buttons = keyboard.InlineKeyboard.SelectMany(row => row).ToList();

        Assert.Single(buttons);
        Assert.Equal("Back to Work", buttons[0].Text);
        Assert.Equal("track:work", buttons[0].CallbackData);
    }

    [Fact]
    public async Task CommuteEnd_ShouldShowStartWorkButton()
    {
        // Arrange
        await RegisterTestUserAsync();

        // Start commute
        await SendTextAsync("/commute");

        // Stop commute (toggle)
        var responses = await SendTextAsync("/commute");

        // Assert
        AssertHasResponse(responses);
        var messageResponse = responses.First(r => r.Type == ResponseType.Message);
        Assert.NotNull(messageResponse.ReplyMarkup);
        var keyboard = (InlineKeyboardMarkup)messageResponse.ReplyMarkup;
        var buttons = keyboard.InlineKeyboard.SelectMany(row => row).ToList();

        Assert.Single(buttons);
        Assert.Equal("Start Work", buttons[0].Text);
        Assert.Equal("track:work", buttons[0].CallbackData);
    }

    [Fact]
    public async Task WorkEnd_ShouldShowCommuteHomeButton()
    {
        // Arrange
        await RegisterTestUserAsync();

        // Start work
        await SendTextAsync("/work");

        // Stop work (toggle)
        var responses = await SendTextAsync("/work");

        // Assert
        AssertHasResponse(responses);
        var messageResponse = responses.First(r => r.Type == ResponseType.Message);
        Assert.NotNull(messageResponse.ReplyMarkup);
        var keyboard = (InlineKeyboardMarkup)messageResponse.ReplyMarkup;
        var buttons = keyboard.InlineKeyboard.SelectMany(row => row).ToList();

        Assert.Single(buttons);
        Assert.Equal("Commute Home", buttons[0].Text);
        Assert.Equal("track:commute", buttons[0].CallbackData);
    }

    [Fact]
    public async Task ClickingTrackButton_ShouldStartState()
    {
        // Arrange
        await RegisterTestUserAsync();

        // Start commute to get buttons
        await SendTextAsync("/commute");

        // Click "Start Work" button
        var responses = await SendCallbackQueryAsync("track:work");

        // Assert
        AssertHasResponse(responses);

        // Verify the message was edited
        var editedMessage = responses.FirstOrDefault(r => r.Type == ResponseType.EditedMessage);
        Assert.NotNull(editedMessage);
        Assert.Contains("work", editedMessage.Text!, StringComparison.OrdinalIgnoreCase);

        // Verify new buttons are shown
        Assert.NotNull(editedMessage.ReplyMarkup);
        var keyboard = (InlineKeyboardMarkup)editedMessage.ReplyMarkup;
        var buttons = keyboard.InlineKeyboard.SelectMany(row => row).ToList();
        Assert.NotEmpty(buttons);

        // Verify callback was answered
        var callbackAnswer = responses.FirstOrDefault(r => r.Type == ResponseType.CallbackAnswer);
        Assert.NotNull(callbackAnswer);
        Assert.Contains("work", callbackAnswer.Text!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ClickingStopButton_ShouldEndState()
    {
        // Arrange
        await RegisterTestUserAsync();

        // Start work to get buttons
        await SendTextAsync("/work");

        // Click "Stop Work" button (toggle behavior)
        var responses = await SendCallbackQueryAsync("track:work");

        // Assert
        AssertHasResponse(responses);

        // Verify the message was edited
        var editedMessage = responses.FirstOrDefault(r => r.Type == ResponseType.EditedMessage);
        Assert.NotNull(editedMessage);
        Assert.Contains("Stopped", editedMessage.Text!, StringComparison.OrdinalIgnoreCase);

        // Verify callback was answered
        var callbackAnswer = responses.FirstOrDefault(r => r.Type == ResponseType.CallbackAnswer);
        Assert.NotNull(callbackAnswer);
    }

    [Fact]
    public async Task ButtonTransitions_ShouldWorkSeamlessly()
    {
        // Arrange
        await RegisterTestUserAsync();

        // Act & Assert: Test a full workflow using only buttons

        // 1. Start commute via command
        var responses1 = await SendTextAsync("/commute");
        var msg1 = responses1.First(r => r.Type == ResponseType.Message);
        var keyboard1 = (InlineKeyboardMarkup)msg1.ReplyMarkup!;
        Assert.Contains(keyboard1.InlineKeyboard.SelectMany(r => r), b => b.Text == "Start Work");

        // 2. Click "Start Work" button
        var responses2 = await SendCallbackQueryAsync("track:work");
        var editedMsg1 = responses2.First(r => r.Type == ResponseType.EditedMessage);
        var keyboard2 = (InlineKeyboardMarkup)editedMsg1.ReplyMarkup!;
        Assert.Contains(keyboard2.InlineKeyboard.SelectMany(r => r), b => b.Text == "Take Lunch");

        // 3. Click "Take Lunch" button
        var responses3 = await SendCallbackQueryAsync("track:lunch");
        var editedMsg2 = responses3.First(r => r.Type == ResponseType.EditedMessage);
        var keyboard3 = (InlineKeyboardMarkup)editedMsg2.ReplyMarkup!;
        Assert.Contains(keyboard3.InlineKeyboard.SelectMany(r => r), b => b.Text == "Back to Work");

        // 4. Click "Back to Work" button
        var responses4 = await SendCallbackQueryAsync("track:work");
        var editedMsg3 = responses4.First(r => r.Type == ResponseType.EditedMessage);
        Assert.Contains("work", editedMsg3.Text!, StringComparison.OrdinalIgnoreCase);

        // All transitions should have worked without errors
        Assert.NotEmpty(responses1);
        Assert.NotEmpty(responses2);
        Assert.NotEmpty(responses3);
        Assert.NotEmpty(responses4);
    }

    [Fact]
    public async Task InvalidCallbackData_ShouldHandleGracefully()
    {
        // Arrange
        await RegisterTestUserAsync();
        await SendTextAsync("/work");

        // Create callback query with invalid data
        var responses = await SendCallbackQueryAsync("track:invalid_state");

        // Assert
        AssertHasResponse(responses);
        var callbackAnswer = responses.First(r => r.Type == ResponseType.CallbackAnswer);
        Assert.Contains("Invalid", callbackAnswer.Text!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CommandsStillWork_WithButtons()
    {
        // Arrange
        const long userId = 99999; // Use unique user ID to avoid conflicts
        await RegisterTestUserAsync(telegramUserId: userId);

        // Act: Test that commands still work alongside buttons
        var responses1 = await SendTextAsync("/commute", userId: userId);
        var responses2 = await SendTextAsync("/work", userId: userId);
        var responses3 = await SendTextAsync("/lunch", userId: userId);
        var responses4 = await SendTextAsync("/commute", userId: userId);

        // Assert: All commands should work
        AssertResponseContains(responses1, "commut");
        AssertResponseContains(responses2, "work");
        AssertResponseContains(responses3, "lunch");
        AssertResponseContains(responses4, "commut");
    }
}
