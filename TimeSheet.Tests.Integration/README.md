# Integration Tests

This project contains integration tests for the TimeSheet application, testing the full system from the Telegram presentation layer down through to persistence.

## Test Harness Architecture

The integration test harness provides a complete DI container with:

- **Real services** - All application and domain services are real (not mocked)
- **In-memory database** - Uses EF Core InMemory provider for fast, isolated tests
- **Mock Telegram client** - Captures bot responses without making real HTTP calls
- **Helper builders** - Fluent API for constructing Telegram Update objects

## Key Components

### TelegramBotTestFixture

Located in `Fixtures/TelegramBotTestFixture.cs`, this fixture:

- Sets up the full DI container with all layers (Application, Infrastructure, Presentation)
- Configures an in-memory database (new instance per fixture)
- Provides a mock ITelegramBotClient that captures responses
- Implements IDisposable for proper resource cleanup

### TelegramBotTestBase

Located in `Base/TelegramBotTestBase.cs`, this base class provides:

- `SendTextAsync()` - Send a text message and get captured responses
- `SendCallbackQueryAsync()` - Send a callback query (inline keyboard press)
- `SendUpdateAsync()` - Send a custom Update object
- `AssertHasResponse()` - Assert bot sent at least one response
- `AssertResponseContains()` - Assert response contains expected text
- `AssertNoResponse()` - Assert bot sent no responses

### UpdateBuilder

Located in `Builders/UpdateBuilder.cs`, this builder provides a fluent API:

```csharp
var update = UpdateBuilder.CreateNew()
    .WithTextMessage("/work", chatId: 12345, userId: 67890)
    .Build();
```

### MockTelegramBotClient

Located in `Mocks/MockTelegramBotClient.cs`, this mock:

- Captures all calls to `SendMessage`, `AnswerCallbackQuery`, etc.
- Stores responses in a list for test assertions
- Returns valid response objects to satisfy the Telegram.Bot API

## Writing Integration Tests

### Basic Pattern

```csharp
public class MyFeatureTests(TelegramBotTestFixture fixture) : TelegramBotTestBase(fixture)
{
    [Fact]
    public async Task CommandName_Scenario_ExpectedBehavior()
    {
        // Arrange
        // Set up test data in database if needed

        // Act
        var responses = await SendTextAsync("/command");

        // Assert
        AssertHasResponse(responses);
        AssertResponseContains(responses, "Expected text");
    }
}
```

### Testing with Database State

```csharp
[Fact]
public async Task Work_WithExistingEntry_UpdatesCorrectly()
{
    // Arrange - seed database
    using (var scope = Fixture.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // Add test entities
        await dbContext.SaveChangesAsync();
    }

    // Act
    var responses = await SendTextAsync("/work");

    // Assert
    AssertHasResponse(responses);

    // Verify database state changed
    using (var scope = Fixture.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // Assert database changes
    }
}
```

### Testing Callback Queries

```csharp
[Fact]
public async Task Edit_ClickMinus5m_AdjustsTime()
{
    // Act
    var responses = await SendCallbackQueryAsync("edit_-5m");

    // Assert
    var callbackResponse = responses.FirstOrDefault(r => r.Type == ResponseType.CallbackAnswer);
    Assert.NotNull(callbackResponse);
}
```

## Running Tests

```bash
# Run all integration tests
dotnet test TimeSheet.Tests.Integration

# Run specific test class
dotnet test --filter "FullyQualifiedName~UpdateHandlerTests"

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"
```

## Future Epics

As subsequent epics implement features, add integration tests following this pattern:

- **Epic 2 (Base Tracking)** - Test `/commute`, `/work`, `/lunch` commands with database verification
- **Epic 3 (Authentication)** - Test registration flow with mnemonic validation
- **Epic 4 (Editing)** - Test `/edit` and `/delete` commands with inline keyboards
- **Epic 5 (Settings)** - Test UTC offset and auto-shutdown configuration
- **Epic 6 (Notifications)** - Test scheduled notifications (may need time mocking)
- **Epic 7 (Reports)** - Test report generation with seeded data

The harness is designed to make these future tests straightforward to write.
