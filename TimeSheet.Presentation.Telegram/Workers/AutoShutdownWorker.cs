using Microsoft.Extensions.Options;
using TimeSheet.Core.Application.Interfaces;
using TimeSheet.Presentation.Telegram.Options;

namespace TimeSheet.Presentation.Telegram.Workers;

/// <summary>
/// Background worker that periodically checks for long-running sessions
/// and automatically shuts them down if they exceed configured limits.
/// </summary>
public sealed class AutoShutdownWorker(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<AutoShutdownWorker> logger,
    IOptions<WorkerOptions> options) : BackgroundService
{
    private TimeSpan CheckInterval => options.Value.AutoShutdownCheckInterval;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Auto-shutdown worker started. Check interval: {Interval} minutes", CheckInterval.TotalMinutes);

        // Wait a bit before starting the first check to allow the bot to fully initialize
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndShutdownSessionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error checking for long-running sessions");
            }

            // Wait for the next check interval
            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    /// <summary>
    /// Checks all active sessions and shuts down any that have exceeded their limits.
    /// </summary>
    private async Task CheckAndShutdownSessionsAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var autoShutdownService = scope.ServiceProvider.GetRequiredService<IAutoShutdownService>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var shutdownSessions = await autoShutdownService.CheckAndShutdownLongRunningSessionsAsync(cancellationToken);

        if (shutdownSessions.Count > 0)
        {
            logger.LogInformation(
                "Auto-shutdown completed for {Count} session(s)",
                shutdownSessions.Count);

            foreach (var session in shutdownSessions)
            {
                var duration = session.EndedAt!.Value - session.StartedAt;

                logger.LogInformation(
                    "Auto-shutdown: User {UserId}, State {State}, Duration {Duration:F2} hours",
                    session.UserId,
                    session.State,
                    duration.TotalHours);

                await notificationService.SendAutoShutdownNotificationAsync(
                    session.UserId,
                    session.State,
                    duration,
                    cancellationToken);
            }
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Auto-shutdown worker is stopping...");
        return base.StopAsync(cancellationToken);
    }
}
