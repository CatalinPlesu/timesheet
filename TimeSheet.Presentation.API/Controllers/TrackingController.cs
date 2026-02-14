using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TimeSheet.Core.Application.Interfaces.Services;
using TimeSheet.Core.Application.Models;
using TimeSheet.Core.Domain.Enums;
using TimeSheet.Core.Domain.Repositories;
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
    private readonly ITimeTrackingService _timeTrackingService;
    private readonly ITrackingSessionRepository _trackingSessionRepository;
    private readonly ILogger<TrackingController> _logger;

    public TrackingController(
        ITimeTrackingService timeTrackingService,
        ITrackingSessionRepository trackingSessionRepository,
        ILogger<TrackingController> logger)
    {
        _timeTrackingService = timeTrackingService ?? throw new ArgumentNullException(nameof(timeTrackingService));
        _trackingSessionRepository = trackingSessionRepository ?? throw new ArgumentNullException(nameof(trackingSessionRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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
    public async Task<ActionResult<CurrentStateResponse>> GetCurrentState(CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetUserIdFromClaims();
            _logger.LogInformation("Getting current tracking state for user {UserId}", userId);

            var activeSession = await _trackingSessionRepository.GetActiveSessionAsync(userId, cancellationToken);

            if (activeSession == null)
            {
                // User is idle
                return Ok(new CurrentStateResponse
                {
                    State = TrackingState.Idle,
                    StartedAt = null,
                    DurationHours = null,
                    CommuteDirection = null
                });
            }

            // Calculate duration
            var duration = DateTime.UtcNow - activeSession.StartedAt;
            var durationHours = (decimal)duration.TotalHours;

            return Ok(new CurrentStateResponse
            {
                State = activeSession.State,
                StartedAt = activeSession.StartedAt,
                DurationHours = durationHours,
                CommuteDirection = activeSession.CommuteDirection
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current tracking state");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while retrieving current state" });
        }
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
    public async Task<ActionResult<TrackingStateResponse>> ToggleState([FromBody] TrackingStateRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            return BadRequest(new { error = "Request body is required" });
        }

        // Validate that the state is not Idle (Idle is only returned, never set)
        if (request.State == TrackingState.Idle)
        {
            return BadRequest(new { error = "Cannot explicitly set Idle state. Use toggle to stop current state." });
        }

        try
        {
            var userId = GetUserIdFromClaims();
            var timestamp = DateTime.UtcNow;

            _logger.LogInformation("Toggling state to {State} for user {UserId}", request.State, userId);

            var result = await _timeTrackingService.StartStateAsync(userId, request.State, timestamp, cancellationToken);

            return Ok(MapToResponse(result, request.State));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling tracking state");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while toggling state" });
        }
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
    public async Task<ActionResult<TrackingStateResponse>> ToggleStateWithOffset([FromBody] TrackingStateWithOffsetRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            return BadRequest(new { error = "Request body is required" });
        }

        // Validate that the state is not Idle
        if (request.State == TrackingState.Idle)
        {
            return BadRequest(new { error = "Cannot explicitly set Idle state. Use toggle to stop current state." });
        }

        try
        {
            var userId = GetUserIdFromClaims();

            // Calculate timestamp with offset
            // Positive offset = action started in the past (subtract minutes)
            // Negative offset = action will start in the future (add minutes)
            var timestamp = DateTime.UtcNow.AddMinutes(-request.OffsetMinutes);

            _logger.LogInformation("Toggling state to {State} for user {UserId} with offset {OffsetMinutes}m (timestamp: {Timestamp})",
                request.State, userId, request.OffsetMinutes, timestamp);

            var result = await _timeTrackingService.StartStateAsync(userId, request.State, timestamp, cancellationToken);

            return Ok(MapToResponse(result, request.State));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling tracking state with offset");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while toggling state" });
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
    /// Maps a TrackingResult to a TrackingStateResponse DTO.
    /// </summary>
    private static TrackingStateResponse MapToResponse(TrackingResult result, TrackingState requestedState)
    {
        return result switch
        {
            TrackingResult.SessionStarted started => new TrackingStateResponse
            {
                NewState = started.StartedSession.State,
                PreviousState = started.EndedSession?.State,
                Message = BuildMessage(started.StartedSession.State, started.StartedSession.StartedAt, started.EndedSession),
                StartedAt = started.StartedSession.StartedAt,
                PreviousSessionDurationHours = started.EndedSession != null
                    ? CalculateDuration(started.EndedSession)
                    : null
            },
            TrackingResult.SessionEnded ended => new TrackingStateResponse
            {
                NewState = TrackingState.Idle,
                PreviousState = ended.EndedSession.State,
                Message = $"Stopped {GetStateDisplayName(ended.EndedSession.State).ToLowerInvariant()} at {ended.EndedSession.EndedAt:HH:mm}",
                StartedAt = null,
                PreviousSessionDurationHours = CalculateDuration(ended.EndedSession)
            },
            TrackingResult.NoChange => new TrackingStateResponse
            {
                NewState = TrackingState.Idle,
                PreviousState = null,
                Message = "No change - already idle",
                StartedAt = null,
                PreviousSessionDurationHours = null
            },
            _ => throw new InvalidOperationException($"Unknown result type: {result.GetType()}")
        };
    }

    /// <summary>
    /// Builds a descriptive message for the tracking state change.
    /// </summary>
    private static string BuildMessage(TrackingState newState, DateTime startedAt, Core.Domain.Entities.TrackingSession? endedSession)
    {
        var stateDisplayName = GetStateDisplayName(newState);
        var timeDisplay = startedAt.ToString("HH:mm");

        if (endedSession != null)
        {
            var previousStateDisplayName = GetStateDisplayName(endedSession.State);
            var duration = CalculateDuration(endedSession);
            return $"Stopped {previousStateDisplayName.ToLowerInvariant()} ({duration:F2}h) and started {stateDisplayName.ToLowerInvariant()} at {timeDisplay}";
        }

        return $"Started {stateDisplayName.ToLowerInvariant()} at {timeDisplay}";
    }

    /// <summary>
    /// Gets a human-readable display name for a tracking state.
    /// </summary>
    private static string GetStateDisplayName(TrackingState state) => state switch
    {
        TrackingState.Idle => "Idle",
        TrackingState.Commuting => "Commuting",
        TrackingState.Working => "Working",
        TrackingState.Lunch => "Lunch",
        _ => state.ToString()
    };

    /// <summary>
    /// Calculates the duration of a tracking session in hours.
    /// </summary>
    private static decimal CalculateDuration(Core.Domain.Entities.TrackingSession session)
    {
        if (session.EndedAt == null)
        {
            return 0;
        }

        var duration = session.EndedAt.Value - session.StartedAt;
        return (decimal)duration.TotalHours;
    }
}
