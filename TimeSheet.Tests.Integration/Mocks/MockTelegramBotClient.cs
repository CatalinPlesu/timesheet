using Moq;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;

namespace TimeSheet.Tests.Integration.Mocks;

/// <summary>
/// Mock implementation of ITelegramBotClient that captures all bot responses.
/// Allows tests to verify what messages/callbacks the bot sends without making real HTTP calls.
/// </summary>
public class MockTelegramBotClient
{
    private readonly Mock<ITelegramBotClient> _mock;
    private readonly List<CapturedResponse> _responses = [];

    public MockTelegramBotClient()
    {
        _mock = new Mock<ITelegramBotClient>();
        SetupMockBehavior();
    }

    /// <summary>
    /// Gets the mock ITelegramBotClient instance to inject into services.
    /// </summary>
    public ITelegramBotClient Client => _mock.Object;

    /// <summary>
    /// Gets all responses captured by this mock (messages, callback answers, etc.).
    /// </summary>
    public IReadOnlyList<CapturedResponse> Responses => _responses.AsReadOnly();

    /// <summary>
    /// Clears all captured responses.
    /// </summary>
    public void ClearResponses() => _responses.Clear();

    private void SetupMockBehavior()
    {
        // Telegram.Bot 22.x uses SendRequest<TResponse> as the main method
        // All other methods (SendMessage, AnswerCallbackQuery, etc.) are extension methods
        // We intercept SendRequest and inspect the request type

        _mock.Setup(x => x.SendRequest<Message>(
                It.IsAny<IRequest<Message>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IRequest<Message> req, CancellationToken ct) =>
            {
                // Check if it's a SendMessageRequest
                if (req is SendMessageRequest sendReq)
                {
                    _responses.Add(new CapturedResponse
                    {
                        Type = ResponseType.Message,
                        ChatId = sendReq.ChatId.Identifier ?? 0,
                        Text = sendReq.Text,
                        ReplyMarkup = sendReq.ReplyMarkup
                    });

                    // Return a minimal Message object (required by the interface)
                    // Note: MessageId is read-only in Telegram.Bot 22.x, so we leave it as default
                    var result = new Message
                    {
                        Date = DateTime.UtcNow,
                        Chat = new Chat { Id = sendReq.ChatId.Identifier ?? 0, Type = Telegram.Bot.Types.Enums.ChatType.Private }
                    };

                    // Try to set MessageId via reflection if possible (readonly property)
                    try
                    {
                        typeof(Message).GetProperty(nameof(Message.MessageId))?.SetValue(result, Random.Shared.Next(1, 999999));
                    }
                    catch
                    {
                        // Ignore if it fails - MessageId isn't critical for tests
                    }

                    return result;
                }

                // Default fallback
                var defaultMsg = new Message
                {
                    Date = DateTime.UtcNow,
                    Chat = new Chat { Id = 0, Type = Telegram.Bot.Types.Enums.ChatType.Private }
                };

                try
                {
                    typeof(Message).GetProperty(nameof(Message.MessageId))?.SetValue(defaultMsg, Random.Shared.Next(1, 999999));
                }
                catch
                {
                    // Ignore
                }

                return defaultMsg;
            });

        // Capture AnswerCallbackQuery calls
        _mock.Setup(x => x.SendRequest<bool>(
                It.IsAny<IRequest<bool>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IRequest<bool> req, CancellationToken ct) =>
            {
                // Check if it's an AnswerCallbackQueryRequest
                if (req is AnswerCallbackQueryRequest answerReq)
                {
                    _responses.Add(new CapturedResponse
                    {
                        Type = ResponseType.CallbackAnswer,
                        CallbackQueryId = answerReq.CallbackQueryId,
                        Text = answerReq.Text,
                        ShowAlert = answerReq.ShowAlert
                    });
                }

                return true;
            });

        // Capture EditMessageText calls (returns Message or bool depending on request type)
        _mock.Setup(x => x.SendRequest<Message>(
                It.Is<IRequest<Message>>(r => r is EditMessageTextRequest),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IRequest<Message> req, CancellationToken ct) =>
            {
                if (req is EditMessageTextRequest editReq)
                {
                    _responses.Add(new CapturedResponse
                    {
                        Type = ResponseType.EditedMessage,
                        ChatId = editReq.ChatId?.Identifier ?? 0,
                        Text = editReq.Text,
                        ReplyMarkup = editReq.ReplyMarkup
                    });

                    var result = new Message
                    {
                        Date = DateTime.UtcNow,
                        Chat = new Chat { Id = editReq.ChatId?.Identifier ?? 0, Type = Telegram.Bot.Types.Enums.ChatType.Private }
                    };

                    try
                    {
                        typeof(Message).GetProperty(nameof(Message.MessageId))?.SetValue(result, editReq.MessageId);
                    }
                    catch
                    {
                        // Ignore
                    }

                    return result;
                }

                return new Message
                {
                    Date = DateTime.UtcNow,
                    Chat = new Chat { Id = 0, Type = Telegram.Bot.Types.Enums.ChatType.Private }
                };
            });
    }
}

/// <summary>
/// Represents a response captured from the mock bot client.
/// </summary>
public class CapturedResponse
{
    public ResponseType Type { get; init; }
    public long ChatId { get; init; }
    public string? Text { get; init; }
    public object? ReplyMarkup { get; init; }
    public string? CallbackQueryId { get; init; }
    public bool? ShowAlert { get; init; }
}

/// <summary>
/// Types of responses the bot can send.
/// </summary>
public enum ResponseType
{
    Message,
    CallbackAnswer,
    EditedMessage
}
