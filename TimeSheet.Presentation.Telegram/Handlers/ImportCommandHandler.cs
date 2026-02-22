using Telegram.Bot;
using Telegram.Bot.Types;
using TimeSheet.Core.Application.Interfaces.Services;
using TimeSheet.Core.Domain.Repositories;

namespace TimeSheet.Presentation.Telegram.Handlers;

/// <summary>
/// Handles the /import command for importing employer attendance data.
/// Syntax:
///   /import &lt;bearer_token&gt;
///   /import &lt;bearer_token&gt; --force
/// </summary>
public class ImportCommandHandler(
    ILogger<ImportCommandHandler> logger,
    IServiceScopeFactory serviceScopeFactory)
{
    private const string UsageText = """
        Usage: /import <bearer_token>

        Your bearer token is a short-lived JWT from your employer's time-tracking system.
        It expires in a few hours, so you'll need to get a fresh one each time.

        Import is rate-limited to once every 7 days.
        Use /import <token> --force to override the rate limit.
        """;

    /// <summary>
    /// Handles the /import command.
    /// </summary>
    public async Task HandleImportAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        var userId = message.From?.Id;
        var username = message.From?.Username;

        if (userId == null)
        {
            logger.LogWarning("Received /import without user ID");
            return;
        }

        var messageText = message.Text ?? string.Empty;

        // Strip the command prefix "/import" and trim
        var afterCommand = messageText.Length > "/import".Length
            ? messageText["/import".Length..].Trim()
            : string.Empty;

        // No arguments — show usage
        if (string.IsNullOrWhiteSpace(afterCommand))
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: UsageText,
                cancellationToken: cancellationToken);
            return;
        }

        // Parse --force flag (case-insensitive)
        bool force = false;
        string bearerToken;

        var forceIndex = afterCommand.IndexOf("--force", StringComparison.OrdinalIgnoreCase);
        if (forceIndex >= 0)
        {
            force = true;
            bearerToken = afterCommand[..forceIndex].Trim();
        }
        else
        {
            bearerToken = afterCommand.Trim();
        }

        if (string.IsNullOrWhiteSpace(bearerToken))
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: UsageText,
                cancellationToken: cancellationToken);
            return;
        }

        // Strip "Bearer " prefix if user pasted the full Authorization header value
        if (bearerToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            bearerToken = bearerToken["Bearer ".Length..].Trim();

        logger.LogInformation(
            "Import command from user {UserId} ({Username}), force={Force}",
            userId.Value,
            username ?? "no username",
            force);

        // Send "⏳ Importing..." message immediately before the async call
        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: "⏳ Importing...",
            cancellationToken: cancellationToken);

        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var importService = scope.ServiceProvider.GetRequiredService<IEmployerImportService>();

            // Resolve the internal user Guid from the Telegram user ID
            var user = await userRepository.GetByTelegramUserIdAsync(userId.Value, cancellationToken);
            if (user == null)
            {
                logger.LogWarning("User {UserId} not found during /import", userId.Value);
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "User not found.",
                    cancellationToken: cancellationToken);
                return;
            }

            var result = await importService.ImportAsync(user.Id, bearerToken, force, cancellationToken);

            string responseText;

            if (result.RateLimited)
            {
                var daysText = result.RateLimitDaysRemaining.HasValue
                    ? $"{result.RateLimitDaysRemaining.Value} day{(result.RateLimitDaysRemaining.Value == 1 ? "" : "s")}"
                    : "some days";

                responseText = $"""
                    ⏳ Import skipped — you imported recently.
                    Next import available in {daysText}.
                    Use /import <token> --force to override.
                    """;
            }
            else if (result.Error != null)
            {
                if (result.Error.Contains("Employer API not configured", StringComparison.OrdinalIgnoreCase) ||
                    result.Error.Contains("BaseUrl", StringComparison.OrdinalIgnoreCase))
                {
                    responseText = """
                        ⚙️ Employer API not configured.
                        Ask your admin to set EmployerApi:BaseUrl in appsettings.
                        """;
                }
                else
                {
                    responseText = $"❌ Import failed: {result.Error}";
                }
            }
            else
            {
                responseText = $"""
                    ✅ Import complete
                    • {result.Imported} records imported
                    • {result.Skipped} days skipped (weekends/holidays/pre-employment)
                    • {result.TotalDaysProcessed} total days processed
                    """;
            }

            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: responseText,
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "Import completed for user {UserId}: imported={Imported}, skipped={Skipped}, total={Total}, rateLimited={RateLimited}, error={Error}",
                userId.Value,
                result.Imported,
                result.Skipped,
                result.TotalDaysProcessed,
                result.RateLimited,
                result.Error);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during /import command for user {UserId}", userId.Value);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "An unexpected error occurred during import. Please try again.",
                cancellationToken: cancellationToken);
        }
    }
}
