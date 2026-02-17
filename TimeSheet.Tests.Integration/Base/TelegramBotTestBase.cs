using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types;
using TimeSheet.Core.Domain.Entities;
using TimeSheet.Presentation.Telegram.Handlers;
using TimeSheet.Tests.Integration.Builders;
using TimeSheet.Tests.Integration.Fixtures;
using TimeSheet.Tests.Integration.Mocks;
using DomainUser = TimeSheet.Core.Domain.Entities.User;

namespace TimeSheet.Tests.Integration.Base;

/// <summary>
/// Base class for Telegram bot integration tests.
/// Provides helper methods to send messages and assert responses.
/// All integration tests should inherit from this class.
/// </summary>
public abstract class TelegramBotTestBase(TelegramBotTestFixture fixture) : IClassFixture<TelegramBotTestFixture>
{
    protected readonly TelegramBotTestFixture Fixture = fixture;

    /// <summary>
    /// Sends a text message to the bot and processes it through the UpdateHandler.
    /// </summary>
    /// <param name="text">The message text (e.g., "/work", "/commute").</param>
    /// <param name="chatId">The chat ID (default: 12345).</param>
    /// <param name="userId">The user ID (default: 67890).</param>
    /// <param name="username">The username (default: "testuser").</param>
    /// <returns>The list of captured responses from the bot.</returns>
    protected async Task<IReadOnlyList<CapturedResponse>> SendTextAsync(
        string text,
        long chatId = 12345,
        long userId = 67890,
        string username = "testuser")
    {
        // Clear previous responses
        Fixture.MockBotClient.ClearResponses();

        // Build update
        var update = UpdateBuilder.CreateNew()
            .WithTextMessage(text, chatId, userId, username)
            .Build();

        // Process update
        var handler = Fixture.GetUpdateHandler();
        await handler.HandleUpdateAsync(Fixture.MockBotClient.Client, update, CancellationToken.None);

        return Fixture.MockBotClient.Responses;
    }

    /// <summary>
    /// Sends a callback query (inline keyboard button press) to the bot.
    /// </summary>
    /// <param name="data">The callback data (e.g., "edit_-5m").</param>
    /// <param name="chatId">The chat ID (default: 12345).</param>
    /// <param name="userId">The user ID (default: 67890).</param>
    /// <param name="username">The username (default: "testuser").</param>
    /// <returns>The list of captured responses from the bot.</returns>
    protected async Task<IReadOnlyList<CapturedResponse>> SendCallbackQueryAsync(
        string data,
        long chatId = 12345,
        long userId = 67890,
        string username = "testuser")
    {
        // Clear previous responses
        Fixture.MockBotClient.ClearResponses();

        // Build update
        var update = UpdateBuilder.CreateNew()
            .WithCallbackQuery(data, chatId, userId, username)
            .Build();

        // Process update
        var handler = Fixture.GetUpdateHandler();
        await handler.HandleUpdateAsync(Fixture.MockBotClient.Client, update, CancellationToken.None);

        return Fixture.MockBotClient.Responses;
    }

    /// <summary>
    /// Sends a custom Update object to the bot.
    /// Use this for advanced scenarios where SendTextAsync/SendCallbackQueryAsync aren't sufficient.
    /// </summary>
    protected async Task<IReadOnlyList<CapturedResponse>> SendUpdateAsync(Update update)
    {
        // Clear previous responses
        Fixture.MockBotClient.ClearResponses();

        // Process update
        var handler = Fixture.GetUpdateHandler();
        await handler.HandleUpdateAsync(Fixture.MockBotClient.Client, update, CancellationToken.None);

        return Fixture.MockBotClient.Responses;
    }

    /// <summary>
    /// Asserts that the bot sent at least one message response.
    /// </summary>
    protected void AssertHasResponse(IReadOnlyList<CapturedResponse> responses)
    {
        Assert.NotEmpty(responses);
    }

    /// <summary>
    /// Asserts that the bot sent a message containing the expected text.
    /// </summary>
    protected void AssertResponseContains(IReadOnlyList<CapturedResponse> responses, string expectedText)
    {
        var messageResponse = responses.FirstOrDefault(r => r.Type == ResponseType.Message);
        Assert.NotNull(messageResponse);
        Assert.Contains(expectedText, messageResponse.Text, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Asserts that the bot sent no responses (useful for testing ignored messages).
    /// </summary>
    protected void AssertNoResponse(IReadOnlyList<CapturedResponse> responses)
    {
        Assert.Empty(responses);
    }

    /// <summary>
    /// Registers a test user directly in the database (bypassing the registration flow).
    /// Use this to set up authenticated test scenarios.
    /// </summary>
    /// <param name="telegramUserId">The Telegram user ID (default: 67890, matches SendTextAsync default).</param>
    /// <param name="telegramUsername">The Telegram username (default: "testuser").</param>
    /// <param name="isAdmin">Whether the user should be an admin (default: false).</param>
    /// <param name="utcOffsetMinutes">The user's UTC offset in minutes (default: 0).</param>
    protected async Task RegisterTestUserAsync(
        long telegramUserId = 67890,
        string telegramUsername = "testuser",
        bool isAdmin = false,
        int utcOffsetMinutes = 0,
        bool clearExisting = true)
    {
        using var scope = Fixture.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TimeSheet.Infrastructure.Persistence.AppDbContext>();

        if (clearExisting)
        {
            // Clean slate: Remove ALL users and sessions to avoid test pollution
            // This ensures each test starts with a fresh database state
            var allSessions = dbContext.Set<TrackingSession>();
            dbContext.Set<TrackingSession>().RemoveRange(allSessions);

            var allUsers = dbContext.Set<DomainUser>();
            dbContext.Set<DomainUser>().RemoveRange(allUsers);

            await dbContext.SaveChangesAsync();
        }

        var user = new DomainUser(
            telegramUserId: telegramUserId,
            telegramUsername: telegramUsername,
            isAdmin: isAdmin,
            utcOffsetMinutes: utcOffsetMinutes);

        dbContext.Set<DomainUser>().Add(user);
        await dbContext.SaveChangesAsync();
    }
}
