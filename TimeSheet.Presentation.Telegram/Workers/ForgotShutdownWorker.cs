using TimeSheet.Core.Application.Interfaces;

namespace TimeSheet.Presentation.Telegram.Workers;

/// <summary>
/// Background worker that periodically checks for long-running sessions
/// and sends reminders to users who may have forgotten to stop tracking.
/// </summary>
public sealed class ForgotShutdownWorker(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<ForgotShutdownWorker> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Forgot-shutdown worker started. Check interval: {Interval} minutes",
            CheckInterval.TotalMinutes);

        // Wait a bit before starting the first check to allow the bot to fully initialize
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndNotifySessionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error checking for forgot-shutdown sessions");
            }

            // Wait for the next check interval
            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    /// <summary>
    /// Checks all active sessions and sends reminders for long-running ones.
    /// </summary>
    private async Task CheckAndNotifySessionsAsync(CancellationToken cancellationToken)
    {
        // Skip weekend notifications (using UTC day of week as acceptable approximation)
        if (DateTime.UtcNow.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return;

        using var scope = serviceScopeFactory.CreateScope();
        var forgotShutdownService = scope.ServiceProvider.GetRequiredService<IForgotShutdownService>();

        var notifiedSessions = await forgotShutdownService.CheckAndNotifyLongRunningSessionsAsync(cancellationToken);

        if (notifiedSessions.Count > 0)
        {
            logger.LogInformation(
                "Sent forgot-shutdown reminders for {Count} session(s)",
                notifiedSessions.Count);

            foreach (var session in notifiedSessions)
            {
                var duration = (DateTime.UtcNow - session.StartedAt).TotalHours;
                logger.LogInformation(
                    "Reminder sent: User {UserId}, State {State}, Duration {Duration:F2} hours",
                    session.UserId,
                    session.State,
                    duration);
            }
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Forgot-shutdown worker is stopping...");
        return base.StopAsync(cancellationToken);
    }
}
