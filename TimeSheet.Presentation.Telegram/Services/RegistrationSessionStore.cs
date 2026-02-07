using System.Collections.Concurrent;

namespace TimeSheet.Presentation.Telegram.Services;

/// <summary>
/// In-memory store for tracking pending registration sessions.
/// When a user validates their mnemonic, they're placed in a pending state
/// where the next message is expected to be their UTC offset.
/// </summary>
public class RegistrationSessionStore
{
    private readonly ConcurrentDictionary<long, PendingRegistration> _pendingSessions = new();

    /// <summary>
    /// Stores a pending registration session.
    /// </summary>
    public void StorePendingRegistration(long telegramUserId, string? telegramUsername, bool isAdmin)
    {
        _pendingSessions[telegramUserId] = new PendingRegistration(
            TelegramUserId: telegramUserId,
            TelegramUsername: telegramUsername,
            IsAdmin: isAdmin,
            CreatedAt: DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Retrieves and removes a pending registration session.
    /// </summary>
    public PendingRegistration? GetAndRemovePendingRegistration(long telegramUserId)
    {
        _pendingSessions.TryRemove(telegramUserId, out var session);
        return session;
    }

    /// <summary>
    /// Checks if a user has a pending registration.
    /// </summary>
    public bool HasPendingRegistration(long telegramUserId)
    {
        return _pendingSessions.ContainsKey(telegramUserId);
    }

    /// <summary>
    /// Removes expired pending registrations (older than 5 minutes).
    /// This is a safety mechanism to prevent stale sessions.
    /// </summary>
    public void CleanupExpiredSessions()
    {
        var expirationThreshold = DateTimeOffset.UtcNow.AddMinutes(-5);
        var expiredKeys = _pendingSessions
            .Where(kvp => kvp.Value.CreatedAt < expirationThreshold)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _pendingSessions.TryRemove(key, out _);
        }
    }
}

/// <summary>
/// Represents a pending registration session.
/// </summary>
public record PendingRegistration(
    long TelegramUserId,
    string? TelegramUsername,
    bool IsAdmin,
    DateTimeOffset CreatedAt);
