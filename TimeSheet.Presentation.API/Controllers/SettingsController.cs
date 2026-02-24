using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TimeSheet.Core.Application.Interfaces.Services;
using TimeSheet.Core.Domain.Enums;
using TimeSheet.Core.Domain.Repositories;
using TimeSheet.Presentation.API.Models.Settings;

namespace TimeSheet.Presentation.API.Controllers;

/// <summary>
/// User settings management endpoints for UTC offset, auto-shutdown, reminders, and targets.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IUserSettingsService _userSettingsService;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(
        IUserRepository userRepository,
        IUserSettingsService userSettingsService,
        ILogger<SettingsController> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _userSettingsService = userSettingsService ?? throw new ArgumentNullException(nameof(userSettingsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    /// <summary>
    /// Gets all user settings for the authenticated user.
    /// </summary>
    /// <returns>User settings including UTC offset, auto-shutdown limits, reminders, and targets.</returns>
    /// <response code="200">Settings retrieved successfully.</response>
    /// <response code="401">Unauthorized - invalid or missing JWT token.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet]
    [ProducesResponseType(typeof(UserSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserSettingsDto>> GetSettings(CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetUserIdFromClaims();
            _logger.LogInformation("Getting settings for user {UserId}", userId);

            var user = await _userRepository.GetByTelegramUserIdAsync(userId, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Authentication Failed",
                    detail: "User not found");
            }

            return Ok(new UserSettingsDto
            {
                UtcOffsetMinutes = user.UtcOffsetMinutes,
                MaxWorkHours = user.MaxWorkHours,
                MaxCommuteHours = user.MaxCommuteHours,
                MaxLunchHours = user.MaxLunchHours,
                LunchReminderHour = user.LunchReminderHour,
                LunchReminderMinute = user.LunchReminderMinute,
                TargetWorkHours = user.TargetWorkHours,
                TargetOfficeHours = user.TargetOfficeHours,
                ForgotShutdownThresholdPercent = user.ForgotShutdownThresholdPercent
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("User ID not found"))
        {
            _logger.LogWarning(ex, "Invalid JWT token - user ID not found in claims");
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Authentication Failed",
                detail: "Invalid authentication token");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user settings");
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An error occurred while retrieving settings");
        }
    }

    /// <summary>
    /// Updates the user's UTC offset.
    /// Used for displaying times in the user's local timezone.
    /// </summary>
    /// <param name="request">The UTC offset update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated user settings.</returns>
    /// <response code="200">UTC offset updated successfully.</response>
    /// <response code="400">Invalid UTC offset value.</response>
    /// <response code="401">Unauthorized - invalid or missing JWT token.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPut("utc-offset")]
    [ProducesResponseType(typeof(UserSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserSettingsDto>> UpdateUtcOffset(
        [FromBody] UpdateUtcOffsetRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetUserIdFromClaims();

            // Validate: UTC offset must be between -12h (-720 min) and +14h (+840 min)
            if (request.UtcOffsetMinutes < -720 || request.UtcOffsetMinutes > 840)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid UTC Offset",
                    detail: "UTC offset must be between -720 minutes (-12h) and +840 minutes (+14h).");
            }

            _logger.LogInformation("Updating UTC offset for user {UserId} to {OffsetMinutes} minutes", userId, request.UtcOffsetMinutes);

            var updatedUser = await _userSettingsService.UpdateUtcOffsetAsync(userId, request.UtcOffsetMinutes, cancellationToken);
            if (updatedUser == null)
            {
                return Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Authentication Failed",
                    detail: "User not found");
            }

            return Ok(MapToDto(updatedUser));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("User ID not found"))
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Authentication Failed",
                detail: "Invalid authentication token");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating UTC offset");
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An error occurred while updating settings");
        }
    }

    /// <summary>
    /// Updates auto-shutdown limits for work, commute, and lunch sessions.
    /// Sessions exceeding these limits will be automatically closed.
    /// </summary>
    /// <param name="request">The auto-shutdown limits update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated user settings.</returns>
    /// <response code="200">Auto-shutdown limits updated successfully.</response>
    /// <response code="400">Invalid limit values (must be positive or null).</response>
    /// <response code="401">Unauthorized - invalid or missing JWT token.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPut("auto-shutdown")]
    [ProducesResponseType(typeof(UserSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserSettingsDto>> UpdateAutoShutdown(
        [FromBody] UpdateAutoShutdownRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetUserIdFromClaims();

            // Validate: all provided values must be positive
            if (request.MaxWorkHours.HasValue && request.MaxWorkHours.Value <= 0)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid Value",
                    detail: "MaxWorkHours must be a positive number.");
            }
            if (request.MaxCommuteHours.HasValue && request.MaxCommuteHours.Value <= 0)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid Value",
                    detail: "MaxCommuteHours must be a positive number.");
            }
            if (request.MaxLunchHours.HasValue && request.MaxLunchHours.Value <= 0)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid Value",
                    detail: "MaxLunchHours must be a positive number.");
            }

            _logger.LogInformation("Updating auto-shutdown limits for user {UserId}", userId);

            // Update each limit in turn; use the last result for the response
            await _userSettingsService.UpdateAutoShutdownLimitAsync(userId, TrackingState.Working, request.MaxWorkHours, cancellationToken);
            await _userSettingsService.UpdateAutoShutdownLimitAsync(userId, TrackingState.Commuting, request.MaxCommuteHours, cancellationToken);
            var updatedUser = await _userSettingsService.UpdateAutoShutdownLimitAsync(userId, TrackingState.Lunch, request.MaxLunchHours, cancellationToken);

            if (updatedUser == null)
            {
                return Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Authentication Failed",
                    detail: "User not found");
            }

            return Ok(MapToDto(updatedUser));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("User ID not found"))
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Authentication Failed",
                detail: "Invalid authentication token");
        }
        catch (ArgumentException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid Value",
                detail: ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating auto-shutdown limits");
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An error occurred while updating settings");
        }
    }

    /// <summary>
    /// Updates the lunch reminder time.
    /// User will receive a reminder at the specified time to take lunch.
    /// </summary>
    /// <param name="request">The lunch reminder update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated user settings.</returns>
    /// <response code="200">Lunch reminder updated successfully.</response>
    /// <response code="400">Invalid hour (0-23) or minute (0-59) values.</response>
    /// <response code="401">Unauthorized - invalid or missing JWT token.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPut("lunch-reminder")]
    [ProducesResponseType(typeof(UserSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserSettingsDto>> UpdateLunchReminder(
        [FromBody] UpdateLunchReminderRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetUserIdFromClaims();

            // Validate hour (0-23) and minute (0-59) when hour is provided
            if (request.Hour.HasValue)
            {
                if (request.Hour.Value < 0 || request.Hour.Value > 23)
                {
                    return Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Invalid Value",
                        detail: "Hour must be between 0 and 23.");
                }
                if (request.Minute < 0 || request.Minute > 59)
                {
                    return Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Invalid Value",
                        detail: "Minute must be between 0 and 59.");
                }
            }

            _logger.LogInformation("Updating lunch reminder for user {UserId} to {Hour}:{Minute}", userId, request.Hour, request.Minute);

            var updatedUser = await _userSettingsService.UpdateLunchReminderTimeAsync(userId, request.Hour, request.Minute, cancellationToken);
            if (updatedUser == null)
            {
                return Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Authentication Failed",
                    detail: "User not found");
            }

            return Ok(MapToDto(updatedUser));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("User ID not found"))
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Authentication Failed",
                detail: "Invalid authentication token");
        }
        catch (ArgumentException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid Value",
                detail: ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating lunch reminder");
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An error occurred while updating settings");
        }
    }

    /// <summary>
    /// Updates the target work and office hours per day.
    /// User will be notified when targets are reached.
    /// </summary>
    /// <param name="request">The target hours update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated user settings.</returns>
    /// <response code="200">Target hours updated successfully.</response>
    /// <response code="400">Invalid target hours value (must be positive or null).</response>
    /// <response code="401">Unauthorized - invalid or missing JWT token.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPut("target-hours")]
    [ProducesResponseType(typeof(UserSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserSettingsDto>> UpdateTargetHours(
        [FromBody] UpdateTargetHoursRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetUserIdFromClaims();

            // Validate: provided hours must be positive
            if (request.TargetWorkHours.HasValue && request.TargetWorkHours.Value <= 0)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid Value",
                    detail: "TargetWorkHours must be a positive number.");
            }
            if (request.TargetOfficeHours.HasValue && request.TargetOfficeHours.Value <= 0)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid Value",
                    detail: "TargetOfficeHours must be a positive number.");
            }

            _logger.LogInformation("Updating target hours for user {UserId}", userId);

            await _userSettingsService.UpdateTargetWorkHoursAsync(userId, request.TargetWorkHours, cancellationToken);
            var updatedUser = await _userSettingsService.UpdateTargetOfficeHoursAsync(userId, request.TargetOfficeHours, cancellationToken);

            if (updatedUser == null)
            {
                return Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Authentication Failed",
                    detail: "User not found");
            }

            return Ok(MapToDto(updatedUser));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("User ID not found"))
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Authentication Failed",
                detail: "Invalid authentication token");
        }
        catch (ArgumentException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid Value",
                detail: ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating target hours");
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An error occurred while updating settings");
        }
    }

    /// <summary>
    /// Updates the forgot-shutdown threshold percentage.
    /// User will be notified when a session exceeds average duration by this percentage.
    /// </summary>
    /// <param name="request">The threshold update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated user settings.</returns>
    /// <response code="200">Threshold updated successfully.</response>
    /// <response code="400">Invalid threshold value (must be greater than 100 or null).</response>
    /// <response code="401">Unauthorized - invalid or missing JWT token.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPut("forgot-threshold")]
    [ProducesResponseType(typeof(UserSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserSettingsDto>> UpdateForgotThreshold(
        [FromBody] UpdateForgotThresholdRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetUserIdFromClaims();

            // Validate: threshold must be > 100 when provided
            if (request.ThresholdPercent.HasValue && request.ThresholdPercent.Value <= 100)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid Value",
                    detail: "ThresholdPercent must be greater than 100.");
            }

            _logger.LogInformation("Updating forgot-shutdown threshold for user {UserId} to {Threshold}%", userId, request.ThresholdPercent);

            var updatedUser = await _userSettingsService.UpdateForgotShutdownThresholdAsync(userId, request.ThresholdPercent, cancellationToken);
            if (updatedUser == null)
            {
                return Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Authentication Failed",
                    detail: "User not found");
            }

            return Ok(MapToDto(updatedUser));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("User ID not found"))
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Authentication Failed",
                detail: "Invalid authentication token");
        }
        catch (ArgumentException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid Value",
                detail: ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating forgot-shutdown threshold");
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An error occurred while updating settings");
        }
    }

    /// <summary>
    /// Updates all user settings in a single request.
    /// </summary>
    /// <param name="request">All settings to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated user settings.</returns>
    /// <response code="200">All settings updated successfully.</response>
    /// <response code="400">One or more invalid setting values.</response>
    /// <response code="401">Unauthorized - invalid or missing JWT token.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPut]
    [ProducesResponseType(typeof(UserSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserSettingsDto>> UpdateAllSettings(
        [FromBody] UpdateAllSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetUserIdFromClaims();

            // Validate UTC offset
            if (request.UtcOffsetMinutes < -720 || request.UtcOffsetMinutes > 840)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid Value",
                    detail: "UtcOffsetMinutes must be between -720 (-12h) and +840 (+14h).");
            }

            // Validate auto-shutdown limits
            if (request.MaxWorkHours.HasValue && request.MaxWorkHours.Value <= 0)
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid Value", detail: "MaxWorkHours must be a positive number.");
            if (request.MaxCommuteHours.HasValue && request.MaxCommuteHours.Value <= 0)
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid Value", detail: "MaxCommuteHours must be a positive number.");
            if (request.MaxLunchHours.HasValue && request.MaxLunchHours.Value <= 0)
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid Value", detail: "MaxLunchHours must be a positive number.");

            // Validate lunch reminder
            if (request.LunchReminderHour.HasValue)
            {
                if (request.LunchReminderHour.Value < 0 || request.LunchReminderHour.Value > 23)
                    return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid Value", detail: "LunchReminderHour must be between 0 and 23.");
                if (request.LunchReminderMinute < 0 || request.LunchReminderMinute > 59)
                    return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid Value", detail: "LunchReminderMinute must be between 0 and 59.");
            }

            // Validate target hours
            if (request.TargetWorkHours.HasValue && request.TargetWorkHours.Value <= 0)
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid Value", detail: "TargetWorkHours must be a positive number.");
            if (request.TargetOfficeHours.HasValue && request.TargetOfficeHours.Value <= 0)
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid Value", detail: "TargetOfficeHours must be a positive number.");

            // Validate forgot-shutdown threshold
            if (request.ForgotShutdownThresholdPercent.HasValue && request.ForgotShutdownThresholdPercent.Value <= 100)
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid Value", detail: "ForgotShutdownThresholdPercent must be greater than 100.");

            _logger.LogInformation("Updating all settings for user {UserId}", userId);

            // Apply all updates in sequence
            await _userSettingsService.UpdateUtcOffsetAsync(userId, request.UtcOffsetMinutes, cancellationToken);
            await _userSettingsService.UpdateAutoShutdownLimitAsync(userId, TrackingState.Working, request.MaxWorkHours, cancellationToken);
            await _userSettingsService.UpdateAutoShutdownLimitAsync(userId, TrackingState.Commuting, request.MaxCommuteHours, cancellationToken);
            await _userSettingsService.UpdateAutoShutdownLimitAsync(userId, TrackingState.Lunch, request.MaxLunchHours, cancellationToken);
            await _userSettingsService.UpdateLunchReminderTimeAsync(userId, request.LunchReminderHour, request.LunchReminderMinute, cancellationToken);
            await _userSettingsService.UpdateTargetWorkHoursAsync(userId, request.TargetWorkHours, cancellationToken);
            await _userSettingsService.UpdateTargetOfficeHoursAsync(userId, request.TargetOfficeHours, cancellationToken);
            var updatedUser = await _userSettingsService.UpdateForgotShutdownThresholdAsync(userId, request.ForgotShutdownThresholdPercent, cancellationToken);

            if (updatedUser == null)
            {
                return Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Authentication Failed",
                    detail: "User not found");
            }

            return Ok(MapToDto(updatedUser));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("User ID not found"))
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Authentication Failed",
                detail: "Invalid authentication token");
        }
        catch (ArgumentException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid Value",
                detail: ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating settings");
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An error occurred while updating settings");
        }
    }

    /// <summary>
    /// Extracts the user ID from JWT claims.
    /// </summary>
    private long GetUserIdFromClaims()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User.FindFirst("telegram_user_id")?.Value;

        if (userIdClaim == null || !long.TryParse(userIdClaim, out var userId))
        {
            throw new InvalidOperationException("User ID not found in JWT claims");
        }

        return userId;
    }

    /// <summary>
    /// Maps a User entity to a UserSettingsDto.
    /// </summary>
    private static UserSettingsDto MapToDto(TimeSheet.Core.Domain.Entities.User user) => new()
    {
        UtcOffsetMinutes = user.UtcOffsetMinutes,
        MaxWorkHours = user.MaxWorkHours,
        MaxCommuteHours = user.MaxCommuteHours,
        MaxLunchHours = user.MaxLunchHours,
        LunchReminderHour = user.LunchReminderHour,
        LunchReminderMinute = user.LunchReminderMinute,
        TargetWorkHours = user.TargetWorkHours,
        TargetOfficeHours = user.TargetOfficeHours,
        ForgotShutdownThresholdPercent = user.ForgotShutdownThresholdPercent
    };
}
