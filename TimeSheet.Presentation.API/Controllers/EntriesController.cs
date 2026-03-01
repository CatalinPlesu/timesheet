using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeSheet.Core.Application.Interfaces.Persistence;
using TimeSheet.Core.Application.Interfaces.Services;
using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.Enums;
using TimeSheet.Core.Domain.Repositories;
using TimeSheet.Presentation.API.Models.Entries;

namespace TimeSheet.Presentation.API.Controllers;

/// <summary>
/// Tracking entry management endpoints for viewing, editing, and deleting entries.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class EntriesController : ControllerBase
{
    private readonly ITrackingSessionRepository _sessionRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EntriesController> _logger;

    public EntriesController(
        ITrackingSessionRepository sessionRepository,
        IJwtTokenService jwtTokenService,
        IUnitOfWork unitOfWork,
        ILogger<EntriesController> logger)
    {
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    /// <summary>
    /// Lists tracking entries for the authenticated user with optional filtering and pagination.
    /// Supports grouping by day, week, month, or year.
    /// </summary>
    /// <param name="request">The filter and pagination parameters.</param>
    /// <returns>A paginated list of tracking entries.</returns>
    /// <response code="200">Entries retrieved successfully.</response>
    /// <response code="400">Invalid filter parameters.</response>
    /// <response code="401">Unauthorized - invalid or missing JWT token.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet]
    [ProducesResponseType(typeof(EntryListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EntryListResponse>> GetEntries([FromQuery] EntryListRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Extract user ID from JWT token
            var userId = _jwtTokenService.GetUserIdFromToken(User);

            // Validate pagination parameters
            if (request.Page < 1)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid Request",
                    detail: "Page number must be at least 1");
            }

            if (request.PageSize < 1 || request.PageSize > 500)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid Request",
                    detail: "Page size must be between 1 and 500");
            }

            // Validate date range
            if (request.StartDate.HasValue && request.EndDate.HasValue && request.StartDate.Value > request.EndDate.Value)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid Request",
                    detail: "Start date must be before or equal to end date");
            }

            // Set default date range if not provided (last 30 days)
            var startDate = request.StartDate ?? DateTime.UtcNow.AddDays(-30).Date;
            var endDate = request.EndDate ?? DateTime.UtcNow.Date.AddDays(1);

            // Get sessions in the specified range
            var allSessions = await _sessionRepository.GetSessionsInRangeAsync(
                userId,
                startDate,
                endDate,
                cancellationToken);

            // Calculate pagination
            var totalCount = allSessions.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

            // Apply pagination
            var sessions = allSessions
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            // Map to DTOs
            var entryDtos = sessions.Select(s => new TrackingEntryDto
            {
                Id = s.Id,
                State = s.State,
                StartedAt = s.StartedAt,
                EndedAt = s.EndedAt,
                DurationHours = s.EndedAt.HasValue
                    ? (decimal)(s.EndedAt.Value - s.StartedAt).TotalHours
                    : null,
                CommuteDirection = s.CommuteDirection,
                IsActive = s.IsActive,
                Note = s.Note
            }).ToList();

            var response = new EntryListResponse
            {
                Entries = entryDtos,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = totalPages
            };

            _logger.LogInformation(
                "User {UserId} retrieved {Count} entries (page {Page}/{TotalPages})",
                userId, entryDtos.Count, request.Page, totalPages);

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid user token");
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Authentication Failed",
                detail: "Invalid user token");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving entries");
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An error occurred while retrieving entries");
        }
    }

    /// <summary>
    /// Gets a single tracking entry by ID.
    /// </summary>
    /// <param name="id">The entry ID.</param>
    /// <returns>The tracking entry.</returns>
    /// <response code="200">Entry retrieved successfully.</response>
    /// <response code="401">Unauthorized - invalid or missing JWT token.</response>
    /// <response code="404">Entry not found or does not belong to the authenticated user.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TrackingEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TrackingEntryDto>> GetEntry(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            // Extract user ID from JWT token
            var userId = _jwtTokenService.GetUserIdFromToken(User);

            // Get the session by ID
            var session = await _sessionRepository.GetByIdAsync(id, cancellationToken);

            if (session == null)
            {
                _logger.LogWarning("User {UserId} attempted to access non-existent entry {EntryId}", userId, id);
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found",
                    detail: "Entry not found");
            }

            // Verify the session belongs to the authenticated user
            if (session.UserId != userId)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to access entry {EntryId} belonging to user {OwnerId}",
                    userId, id, session.UserId);
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found",
                    detail: "Entry not found");
            }

            // Map to DTO
            var entryDto = new TrackingEntryDto
            {
                Id = session.Id,
                State = session.State,
                StartedAt = session.StartedAt,
                EndedAt = session.EndedAt,
                DurationHours = session.EndedAt.HasValue
                    ? (decimal)(session.EndedAt.Value - session.StartedAt).TotalHours
                    : null,
                CommuteDirection = session.CommuteDirection,
                IsActive = session.IsActive,
                Note = session.Note
            };

            _logger.LogInformation("User {UserId} retrieved entry {EntryId}", userId, id);

            return Ok(entryDto);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid user token");
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Authentication Failed",
                detail: "Invalid user token");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving entry {EntryId}", id);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An error occurred while retrieving the entry");
        }
    }

    /// <summary>
    /// Updates a tracking entry's time by adjusting the start and/or end time.
    /// Positive adjustment moves the time later, negative moves it earlier.
    /// At least one of StartAdjustmentMinutes or EndAdjustmentMinutes must be provided.
    /// </summary>
    /// <param name="id">The entry ID.</param>
    /// <param name="request">The adjustment request.</param>
    /// <returns>The updated tracking entry.</returns>
    /// <response code="200">Entry updated successfully.</response>
    /// <response code="400">Invalid adjustment or entry state.</response>
    /// <response code="401">Unauthorized - invalid or missing JWT token.</response>
    /// <response code="404">Entry not found or does not belong to the authenticated user.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(TrackingEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TrackingEntryDto>> UpdateEntry(Guid id, [FromBody] EntryUpdateRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Extract user ID from JWT token
            var userId = _jwtTokenService.GetUserIdFromToken(User);

            // Validate: at least one field must be provided
            if (request.StartAdjustmentMinutes == null && request.EndAdjustmentMinutes == null && !request.UpdateNote)
                return Problem(statusCode: 400, title: "Invalid Request", detail: "At least one field to update must be provided");
            if (request.StartAdjustmentMinutes == 0 || request.EndAdjustmentMinutes == 0)
                return Problem(statusCode: 400, title: "Invalid Request", detail: "Adjustment cannot be zero");

            // Get the session by ID
            var session = await _sessionRepository.GetByIdAsync(id, cancellationToken);

            if (session == null)
            {
                _logger.LogWarning("User {UserId} attempted to update non-existent entry {EntryId}", userId, id);
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found",
                    detail: "Entry not found");
            }

            // Verify the session belongs to the authenticated user
            if (session.UserId != userId)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to update entry {EntryId} belonging to user {OwnerId}",
                    userId, id, session.UserId);
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found",
                    detail: "Entry not found");
            }

            // Apply adjustments
            if (request.StartAdjustmentMinutes.HasValue)
            {
                try { session.AdjustStartTime(request.StartAdjustmentMinutes.Value); }
                catch (ArgumentException ex)
                {
                    _logger.LogWarning(ex, "User {UserId} attempted invalid start adjustment on entry {EntryId}", userId, id);
                    return Problem(statusCode: 400, detail: ex.Message);
                }
            }
            if (request.EndAdjustmentMinutes.HasValue)
            {
                try { session.AdjustEndTime(request.EndAdjustmentMinutes.Value); }
                catch (InvalidOperationException ex)
                {
                    _logger.LogWarning(ex, "User {UserId} attempted to adjust active entry {EntryId}", userId, id);
                    return Problem(statusCode: 400, detail: ex.Message);
                }
                catch (ArgumentException ex)
                {
                    _logger.LogWarning(ex, "User {UserId} attempted invalid end adjustment on entry {EntryId}", userId, id);
                    return Problem(statusCode: 400, detail: ex.Message);
                }
            }

            // Apply note update if requested
            if (request.UpdateNote)
            {
                session.UpdateNote(request.Note);
                _logger.LogInformation(
                    "User {UserId} {Action} note on entry {EntryId}",
                    userId,
                    string.IsNullOrWhiteSpace(request.Note) ? "cleared" : "set",
                    id);
            }

            // Update the session in the repository
            _sessionRepository.Update(session);
            await _unitOfWork.CompleteAsync(cancellationToken);

            // Map to DTO
            var entryDto = new TrackingEntryDto
            {
                Id = session.Id,
                State = session.State,
                StartedAt = session.StartedAt,
                EndedAt = session.EndedAt,
                DurationHours = session.EndedAt.HasValue
                    ? (decimal)(session.EndedAt.Value - session.StartedAt).TotalHours
                    : null,
                CommuteDirection = session.CommuteDirection,
                IsActive = session.IsActive,
                Note = session.Note
            };

            _logger.LogInformation(
                "User {UserId} adjusted entry {EntryId} (start: {StartMinutes}, end: {EndMinutes})",
                userId, id, request.StartAdjustmentMinutes, request.EndAdjustmentMinutes);

            return Ok(entryDto);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid user token");
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Authentication Failed",
                detail: "Invalid user token");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating entry {EntryId}", id);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An error occurred while updating the entry");
        }
    }

    /// <summary>
    /// Deletes a tracking entry.
    /// </summary>
    /// <param name="id">The entry ID.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Entry deleted successfully.</response>
    /// <response code="401">Unauthorized - invalid or missing JWT token.</response>
    /// <response code="404">Entry not found or does not belong to the authenticated user.</response>
    /// <response code="500">Internal server error.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteEntry(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            // Extract user ID from JWT token
            var userId = _jwtTokenService.GetUserIdFromToken(User);

            // Get the session by ID
            var session = await _sessionRepository.GetByIdAsync(id, cancellationToken);

            if (session == null)
            {
                _logger.LogWarning("User {UserId} attempted to delete non-existent entry {EntryId}", userId, id);
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found",
                    detail: "Entry not found");
            }

            // Verify the session belongs to the authenticated user
            if (session.UserId != userId)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to delete entry {EntryId} belonging to user {OwnerId}",
                    userId, id, session.UserId);
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found",
                    detail: "Entry not found");
            }

            // Delete the session
            _sessionRepository.Remove(session);
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("User {UserId} deleted entry {EntryId}", userId, id);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid user token");
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Authentication Failed",
                detail: "Invalid user token");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting entry {EntryId}", id);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An error occurred while deleting the entry");
        }
    }

    /// <summary>
    /// Updates the note on a tracking entry. Send null or empty string to clear the note.
    /// </summary>
    /// <param name="id">The entry ID.</param>
    /// <param name="request">The note update request.</param>
    /// <returns>The updated tracking entry.</returns>
    /// <response code="200">Note updated successfully.</response>
    /// <response code="401">Unauthorized - invalid or missing JWT token.</response>
    /// <response code="404">Entry not found or does not belong to the authenticated user.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPatch("{id}/note")]
    [ProducesResponseType(typeof(TrackingEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TrackingEntryDto>> UpdateEntryNote(Guid id, [FromBody] EntryNoteRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = _jwtTokenService.GetUserIdFromToken(User);

            var session = await _sessionRepository.GetByIdAsync(id, cancellationToken);

            if (session == null)
            {
                _logger.LogWarning("User {UserId} attempted to update note on non-existent entry {EntryId}", userId, id);
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found",
                    detail: "Entry not found");
            }

            if (session.UserId != userId)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to update note on entry {EntryId} belonging to user {OwnerId}",
                    userId, id, session.UserId);
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found",
                    detail: "Entry not found");
            }

            session.UpdateNote(request.Note);
            _sessionRepository.Update(session);
            await _unitOfWork.CompleteAsync(cancellationToken);

            var entryDto = new TrackingEntryDto
            {
                Id = session.Id,
                State = session.State,
                StartedAt = session.StartedAt,
                EndedAt = session.EndedAt,
                DurationHours = session.EndedAt.HasValue
                    ? (decimal)(session.EndedAt.Value - session.StartedAt).TotalHours
                    : null,
                CommuteDirection = session.CommuteDirection,
                IsActive = session.IsActive,
                Note = session.Note
            };

            _logger.LogInformation(
                "User {UserId} {Action} note on entry {EntryId}",
                userId,
                string.IsNullOrWhiteSpace(request.Note) ? "cleared" : "set",
                id);

            return Ok(entryDto);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid user token");
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Authentication Failed",
                detail: "Invalid user token");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating note on entry {EntryId}", id);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An error occurred while updating the note");
        }
    }

    /// <summary>
    /// Sets the absolute start and/or end time of an entry directly (HH:MM picker).
    /// For active sessions, only StartedAt may be set. At least one field must be provided.
    /// </summary>
    /// <param name="id">The entry ID.</param>
    /// <param name="request">The set-times request.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Times updated successfully.</response>
    /// <response code="400">Invalid request or conflicting times.</response>
    /// <response code="401">Unauthorized.</response>
    /// <response code="404">Entry not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPatch("{id}/times")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> SetEntryTimes(Guid id, [FromBody] EntrySetTimesRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = _jwtTokenService.GetUserIdFromToken(User);

            if (request.StartedAt == null && request.EndedAt == null)
                return Problem(statusCode: 400, title: "Invalid Request", detail: "At least one of StartedAt or EndedAt must be provided");

            var session = await _sessionRepository.GetByIdAsync(id, cancellationToken);

            if (session == null)
            {
                _logger.LogWarning("User {UserId} attempted to set times on non-existent entry {EntryId}", userId, id);
                return Problem(statusCode: StatusCodes.Status404NotFound, title: "Not Found", detail: "Entry not found");
            }

            if (session.UserId != userId)
            {
                _logger.LogWarning("User {UserId} attempted to set times on entry {EntryId} belonging to user {OwnerId}", userId, id, session.UserId);
                return Problem(statusCode: StatusCodes.Status404NotFound, title: "Not Found", detail: "Entry not found");
            }

            if (request.EndedAt.HasValue && session.IsActive)
                return Problem(statusCode: 400, title: "Invalid Request", detail: "Cannot set end time on an active session");

            if (request.StartedAt.HasValue && request.EndedAt.HasValue && request.StartedAt.Value >= request.EndedAt.Value)
                return Problem(statusCode: 400, title: "Invalid Request", detail: "StartedAt must be before EndedAt");

            // Also check against existing end time if only setting start
            if (request.StartedAt.HasValue && !request.EndedAt.HasValue && session.EndedAt.HasValue && request.StartedAt.Value >= session.EndedAt.Value)
                return Problem(statusCode: 400, title: "Invalid Request", detail: "StartedAt must be before the existing end time");

            // Check against existing start time if only setting end
            if (!request.StartedAt.HasValue && request.EndedAt.HasValue && request.EndedAt.Value <= session.StartedAt)
                return Problem(statusCode: 400, title: "Invalid Request", detail: "EndedAt must be after the existing start time");

            try
            {
                session.SetTimes(request.StartedAt, request.EndedAt);
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                _logger.LogWarning(ex, "User {UserId} attempted invalid SetTimes on entry {EntryId}", userId, id);
                return Problem(statusCode: 400, detail: ex.Message);
            }

            _sessionRepository.Update(session);
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("User {UserId} set times on entry {EntryId} (start: {Start}, end: {End})", userId, id, request.StartedAt, request.EndedAt);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid user token");
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Authentication Failed", detail: "Invalid user token");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting times on entry {EntryId}", id);
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Internal Server Error", detail: "An error occurred while setting times");
        }
    }

    /// <summary>
    /// Creates a completed tracking entry with explicit start and end times.
    /// Used for adding past entries that were not recorded in real-time.
    /// </summary>
    /// <param name="request">The entry creation request.</param>
    /// <returns>The newly created entry.</returns>
    /// <response code="201">Entry created successfully.</response>
    /// <response code="400">Invalid request data.</response>
    /// <response code="401">Unauthorized.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost]
    [ProducesResponseType(typeof(TrackingEntryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TrackingEntryDto>> CreateEntry([FromBody] EntryCreateRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = _jwtTokenService.GetUserIdFromToken(User);

            // Validate state
            if (!Enum.TryParse<TrackingState>(request.State, ignoreCase: true, out var state) || state == TrackingState.Idle)
                return Problem(statusCode: 400, title: "Invalid Request", detail: "State must be one of: Working, Commuting, Lunch");

            // Validate times
            if (request.StartedAt >= request.EndedAt)
                return Problem(statusCode: 400, title: "Invalid Request", detail: "StartedAt must be before EndedAt");

            // Validate commute direction
            CommuteDirection? commuteDirection = null;
            if (state == TrackingState.Commuting)
            {
                if (string.IsNullOrWhiteSpace(request.CommuteDirection))
                    return Problem(statusCode: 400, title: "Invalid Request", detail: "CommuteDirection is required when State is Commuting");

                if (!Enum.TryParse<CommuteDirection>(request.CommuteDirection, ignoreCase: true, out var dir))
                    return Problem(statusCode: 400, title: "Invalid Request", detail: "CommuteDirection must be ToWork or ToHome");

                commuteDirection = dir;
            }

            // Create the completed session
            var session = new TrackingSession(userId, state, request.StartedAt, commuteDirection);
            session.End(request.EndedAt);

            if (!string.IsNullOrWhiteSpace(request.Note))
                session.UpdateNote(request.Note);

            await _sessionRepository.AddAsync(session, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            var entryDto = new TrackingEntryDto
            {
                Id = session.Id,
                State = session.State,
                StartedAt = session.StartedAt,
                EndedAt = session.EndedAt,
                DurationHours = (decimal)(session.EndedAt!.Value - session.StartedAt).TotalHours,
                CommuteDirection = session.CommuteDirection,
                IsActive = session.IsActive,
                Note = session.Note
            };

            _logger.LogInformation("User {UserId} created past entry {EntryId} ({State} {Start} – {End})", userId, session.Id, state, request.StartedAt, request.EndedAt);

            return CreatedAtAction(nameof(GetEntry), new { id = session.Id }, entryDto);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid user token");
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Authentication Failed", detail: "Invalid user token");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating entry");
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Internal Server Error", detail: "An error occurred while creating the entry");
        }
    }
}
