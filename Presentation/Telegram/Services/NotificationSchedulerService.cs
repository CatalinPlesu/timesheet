using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TimeSheet.Core.Application.Notifications;
using TimeSheet.Core.Domain.Enums;
using TimeSheet.Core.Domain.Repositories;

namespace TimeSheet.Presentation.Telegram.Services;

public class NotificationSchedulerService : BackgroundService
{
  private readonly IServiceProvider _services;
  private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);
  private readonly HashSet<string> _sentNotificationsToday = new();
  private DateOnly _currentDay = DateOnly.FromDateTime(DateTime.UtcNow);

  public NotificationSchedulerService(IServiceProvider services)
  {
    _services = services ?? throw new ArgumentNullException(nameof(services));
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    Console.WriteLine("Notification scheduler started");

    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        await CheckAndSendNotifications(stoppingToken);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error in notification scheduler: {ex.Message}");
      }

      await Task.Delay(_checkInterval, stoppingToken);
    }
  }

  private async Task CheckAndSendNotifications(CancellationToken cancellationToken)
  {
    using var scope = _services.CreateScope();
    var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
    var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

    var users = await userRepo.GetAllAsync(cancellationToken);
    var now = DateTime.UtcNow;
    var today = DateOnly.FromDateTime(now);
    
    // Reset notification tracking on new day
    if (today != _currentDay)
    {
      _currentDay = today;
      _sentNotificationsToday.Clear();
    }

    foreach (var user in users)
    {
      // Get user's local time
      var userLocalTime = now.AddHours(user.UtcOffsetHours);
      var currentTime = TimeOnly.FromDateTime(userLocalTime);

      var prefs = user.NotificationPreferences;
      var telegramId = GetTelegramId(user);
      if (!telegramId.HasValue) continue;

      // Check lunch reminder
      if (prefs.LunchReminderEnabled && IsTimeToNotify(currentTime, prefs.LunchReminderTime))
      {
        var notificationKey = $"{telegramId}-lunch-{today}";
        if (!_sentNotificationsToday.Contains(notificationKey))
        {
          await notificationService.SendNotificationAsync(
            telegramId.Value,
            NotificationType.LunchReminder,
            "Time for lunch! Don't forget to use /lunch to track your break.",
            cancellationToken
          );
          _sentNotificationsToday.Add(notificationKey);
        }
      }

      // Check end of day reminder
      if (prefs.EndOfDayReminderEnabled && IsTimeToNotify(currentTime, prefs.EndOfDayReminderTime))
      {
        var notificationKey = $"{telegramId}-endofday-{today}";
        if (!_sentNotificationsToday.Contains(notificationKey))
        {
          await notificationService.SendNotificationAsync(
            telegramId.Value,
            NotificationType.EndOfDayReminder,
            "End of day approaching! Remember to use /home to track your commute.",
            cancellationToken
          );
          _sentNotificationsToday.Add(notificationKey);
        }
      }
    }
  }

  private bool IsTimeToNotify(TimeOnly currentTime, TimeOnly targetTime)
  {
    // Check if current time is within 1 minute of target time
    var diff = Math.Abs((currentTime.ToTimeSpan() - targetTime.ToTimeSpan()).TotalMinutes);
    return diff < 1;
  }

  private long? GetTelegramId(TimeSheet.Core.Domain.Entities.User user)
  {
    var telegramIdentity = user.Identities.FirstOrDefault(i => i.IdentityProvider == IdentityProvider.Telegram);
    return telegramIdentity?.Id;
  }
}
