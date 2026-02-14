using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    public Task<ActionResult<UserSettingsDto>> GetSettings()
    {
        throw new NotImplementedException("Get settings endpoint will be implemented in a future task");
    }

    /// <summary>
    /// Updates the user's UTC offset.
    /// Used for displaying times in the user's local timezone.
    /// </summary>
    /// <param name="request">The UTC offset update request.</param>
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
    public Task<ActionResult<UserSettingsDto>> UpdateUtcOffset([FromBody] UpdateUtcOffsetRequest request)
    {
        throw new NotImplementedException("Update UTC offset endpoint will be implemented in a future task");
    }

    /// <summary>
    /// Updates auto-shutdown limits for work, commute, and lunch sessions.
    /// Sessions exceeding these limits will be automatically closed.
    /// </summary>
    /// <param name="request">The auto-shutdown limits update request.</param>
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
    public Task<ActionResult<UserSettingsDto>> UpdateAutoShutdown([FromBody] UpdateAutoShutdownRequest request)
    {
        throw new NotImplementedException("Update auto-shutdown endpoint will be implemented in a future task");
    }

    /// <summary>
    /// Updates the lunch reminder time.
    /// User will receive a reminder at the specified time to take lunch.
    /// </summary>
    /// <param name="request">The lunch reminder update request.</param>
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
    public Task<ActionResult<UserSettingsDto>> UpdateLunchReminder([FromBody] UpdateLunchReminderRequest request)
    {
        throw new NotImplementedException("Update lunch reminder endpoint will be implemented in a future task");
    }

    /// <summary>
    /// Updates the target work hours per day.
    /// User will be notified when this target is reached.
    /// </summary>
    /// <param name="request">The target hours update request.</param>
    /// <returns>Updated user settings.</returns>
    /// <response code="200">Target work hours updated successfully.</response>
    /// <response code="400">Invalid target hours value (must be positive or null).</response>
    /// <response code="401">Unauthorized - invalid or missing JWT token.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPut("target-hours")]
    [ProducesResponseType(typeof(UserSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<ActionResult<UserSettingsDto>> UpdateTargetHours([FromBody] UpdateTargetHoursRequest request)
    {
        throw new NotImplementedException("Update target hours endpoint will be implemented in a future task");
    }

    /// <summary>
    /// Updates the forgot-shutdown threshold percentage.
    /// User will be notified when a session exceeds average duration by this percentage.
    /// </summary>
    /// <param name="request">The threshold update request.</param>
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
    public Task<ActionResult<UserSettingsDto>> UpdateForgotThreshold([FromBody] UpdateForgotThresholdRequest request)
    {
        throw new NotImplementedException("Update forgot threshold endpoint will be implemented in a future task");
    }
}
