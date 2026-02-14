using TimeSheet.Core.Application.Interfaces.Services;
using TimeSheet.Core.Domain.Enums;

namespace TimeSheet.Presentation.API.Services;

/// <summary>
/// Stub implementation of INotificationService for the API.
/// The API doesn't send notifications (that's the Telegram bot's job),
/// but some application services require this dependency.
/// </summary>
public sealed class StubNotificationService : INotificationService
{
    public Task SendLunchReminderAsync(long telegramUserId, CancellationToken cancellationToken = default)
    {
        // API doesn't send notifications - this is a no-op
        return Task.CompletedTask;
    }

    public Task SendWorkHoursCompleteAsync(long telegramUserId, decimal targetHours, decimal actualHours, CancellationToken cancellationToken = default)
    {
        // API doesn't send notifications - this is a no-op
        return Task.CompletedTask;
    }

    public Task SendForgotShutdownReminderAsync(long telegramUserId, TrackingState state, decimal currentDuration, decimal averageDuration, CancellationToken cancellationToken = default)
    {
        // API doesn't send notifications - this is a no-op
        return Task.CompletedTask;
    }

    public Task SendAutoShutdownNotificationAsync(long telegramUserId, TrackingState state, TimeSpan duration, CancellationToken cancellationToken = default)
    {
        // API doesn't send notifications - this is a no-op
        return Task.CompletedTask;
    }
}
