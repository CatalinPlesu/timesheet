using Telegram.Bot;
using TimeSheet.Core.Application.Notifications;
using TimeSheet.Core.Domain.Enums;

namespace TimeSheet.Presentation.Telegram.Services;

public class TelegramNotificationService : INotificationService
{
  private readonly ITelegramBotClient _bot;

  public TelegramNotificationService(ITelegramBotClient bot)
  {
    _bot = bot ?? throw new ArgumentNullException(nameof(bot));
  }

  public async Task SendNotificationAsync(long externalUserId, NotificationType type, string message, CancellationToken cancellationToken = default)
  {
    var emoji = type switch
    {
      NotificationType.LunchReminder => "🍽️",
      NotificationType.EndOfDayReminder => "🏠",
      NotificationType.ForgotToClockOut => "⚠️",
      NotificationType.DailyGoalAchieved => "🎉",
      _ => "📢"
    };

    var formattedMessage = $"{emoji} {message}";
    
    try
    {
      await _bot.SendMessage(externalUserId, formattedMessage, cancellationToken: cancellationToken);
    }
    catch (Exception ex)
    {
      // Log error but don't throw to prevent notification failures from breaking the app
      Console.WriteLine($"Failed to send notification to user {externalUserId}: {ex.Message}");
    }
  }
}
