using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TimeSheet.Tests.Integration.Builders;

/// <summary>
/// Builder for constructing Telegram Update objects for testing.
/// Provides fluent API to create text messages, callback queries, etc.
/// </summary>
public class UpdateBuilder
{
    private int _updateId = 1;
    private Message? _message;
    private CallbackQuery? _callbackQuery;

    /// <summary>
    /// Creates a new UpdateBuilder instance.
    /// </summary>
    public static UpdateBuilder CreateNew() => new();

    /// <summary>
    /// Sets the update ID (auto-increments if not specified).
    /// </summary>
    public UpdateBuilder WithUpdateId(int updateId)
    {
        _updateId = updateId;
        return this;
    }

    /// <summary>
    /// Adds a text message to the update.
    /// </summary>
    /// <param name="text">The message text (e.g., "/work", "Hello").</param>
    /// <param name="chatId">The chat ID (default: 12345).</param>
    /// <param name="userId">The user ID (default: 67890).</param>
    /// <param name="username">The username (default: "testuser").</param>
    public UpdateBuilder WithTextMessage(
        string text,
        long chatId = 12345,
        long userId = 67890,
        string username = "testuser")
    {
        // Create message using constructor/initializer - some properties in Telegram.Bot types
        // may be init-only or set via constructor parameters
        var messageId = Random.Shared.Next(1, 999999);
        var date = DateTime.UtcNow;

        _message = new Message
        {
            Chat = new Chat
            {
                Id = chatId,
                Type = ChatType.Private
            },
            From = new User
            {
                Id = userId,
                Username = username,
                IsBot = false
            },
            Text = text,
            Date = date
        };

        // Try to set MessageId via reflection (may fail for readonly properties in newer Telegram.Bot versions)
        try
        {
            var prop = typeof(Message).GetProperty(nameof(Message.MessageId));
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(_message, messageId);
            }
        }
        catch
        {
            // Ignore - MessageId not critical for tests
        }

        return this;
    }

    /// <summary>
    /// Adds a callback query (inline keyboard button press) to the update.
    /// </summary>
    /// <param name="data">The callback data (e.g., "edit_-5m").</param>
    /// <param name="chatId">The chat ID (default: 12345).</param>
    /// <param name="userId">The user ID (default: 67890).</param>
    /// <param name="username">The username (default: "testuser").</param>
    public UpdateBuilder WithCallbackQuery(
        string data,
        long chatId = 12345,
        long userId = 67890,
        string username = "testuser")
    {
        var messageId = Random.Shared.Next(1, 999999);
        var date = DateTime.UtcNow;

        var message = new Message
        {
            Date = date,
            Chat = new Chat
            {
                Id = chatId,
                Type = ChatType.Private
            }
        };

        // Try to set MessageId via reflection (may fail for readonly properties in newer Telegram.Bot versions)
        try
        {
            var prop = typeof(Message).GetProperty(nameof(Message.MessageId));
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(message, messageId);
            }
        }
        catch
        {
            // Ignore - MessageId not critical for tests
        }

        _callbackQuery = new CallbackQuery
        {
            Id = Guid.NewGuid().ToString(),
            From = new User
            {
                Id = userId,
                Username = username,
                IsBot = false
            },
            Message = message,
            Data = data
        };

        return this;
    }

    /// <summary>
    /// Builds the Update object.
    /// </summary>
    public Update Build()
    {
        var update = new Update
        {
            Id = _updateId
        };

        if (_message is not null)
        {
            update.Message = _message;
        }

        if (_callbackQuery is not null)
        {
            update.CallbackQuery = _callbackQuery;
        }

        return update;
    }
}
