using TimeSheet.Core.Domain.Enums;

namespace TimeSheet.Core.Application.Notifications;

public interface INotificationService
{
  Task SendNotificationAsync(long externalUserId, NotificationType type, string message, CancellationToken cancellationToken = default);
}
