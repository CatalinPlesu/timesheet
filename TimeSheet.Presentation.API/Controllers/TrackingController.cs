using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeSheet.Presentation.API.Models.Tracking;

namespace TimeSheet.Presentation.API.Controllers;

/// <summary>
/// Tracking state management endpoints for starting/stopping work, commute, and lunch tracking.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class TrackingController : ControllerBase
{
    /// <summary>
    /// Gets the current tracking state for the authenticated user.
    /// </summary>
    /// <returns>The current tracking state including duration and commute direction if applicable.</returns>
    /// <response code="200">Current state retrieved successfully.</response>
    /// <response code="401">Unauthorized - invalid or missing JWT token.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("current")]
    [ProducesResponseType(typeof(CurrentStateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<ActionResult<CurrentStateResponse>> GetCurrentState()
    {
        throw new NotImplementedException("Get current state endpoint will be implemented in TimeSheet-zei.3");
    }

    /// <summary>
    /// Toggles tracking state (commute/work/lunch).
    /// If the requested state is already active, it stops it (toggle behavior).
    /// If a different state is active, it stops the current state and starts the new one.
    /// </summary>
    /// <param name="request">The tracking state request.</param>
    /// <returns>The result of the toggle operation including new state and previous session info.</returns>
    /// <response code="200">State toggled successfully.</response>
    /// <response code="400">Invalid state or request.</response>
    /// <response code="401">Unauthorized - invalid or missing JWT token.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost("toggle")]
    [ProducesResponseType(typeof(TrackingStateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<ActionResult<TrackingStateResponse>> ToggleState([FromBody] TrackingStateRequest request)
    {
        throw new NotImplementedException("Toggle state endpoint will be implemented in TimeSheet-zei.3");
    }

    /// <summary>
    /// Toggles tracking state with a time offset.
    /// Allows backdating or scheduling state changes.
    /// Positive offset values mean the action started in the past.
    /// Negative offset values mean the action will start in the future.
    /// </summary>
    /// <param name="request">The tracking state request with time offset.</param>
    /// <returns>The result of the toggle operation including new state and previous session info.</returns>
    /// <response code="200">State toggled successfully with offset.</response>
    /// <response code="400">Invalid state, offset, or request.</response>
    /// <response code="401">Unauthorized - invalid or missing JWT token.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost("toggle-with-offset")]
    [ProducesResponseType(typeof(TrackingStateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<ActionResult<TrackingStateResponse>> ToggleStateWithOffset([FromBody] TrackingStateWithOffsetRequest request)
    {
        throw new NotImplementedException("Toggle state with offset endpoint will be implemented in TimeSheet-zei.3");
    }
}
