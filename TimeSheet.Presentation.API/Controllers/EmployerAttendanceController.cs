using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeSheet.Core.Application.Interfaces.Services;
using TimeSheet.Core.Domain.Repositories;
using TimeSheet.Presentation.API.Models.EmployerAttendance;

namespace TimeSheet.Presentation.API.Controllers;

/// <summary>
/// Employer attendance endpoints for accessing imported door clock-in/out data.
/// </summary>
[ApiController]
[Route("api/employer-attendance")]
[Produces("application/json")]
[Authorize]
public class EmployerAttendanceController : ControllerBase
{
    private readonly IEmployerAttendanceRepository _employerAttendanceRepository;
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<EmployerAttendanceController> _logger;

    public EmployerAttendanceController(
        IEmployerAttendanceRepository employerAttendanceRepository,
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        ILogger<EmployerAttendanceController> logger)
    {
        _employerAttendanceRepository = employerAttendanceRepository ?? throw new ArgumentNullException(nameof(employerAttendanceRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the employer attendance records for the authenticated user within a date range.
    /// Data is sourced from imported door clock-in/out records (e.g., from Timily).
    /// Returns an empty records list when no data has been imported — not an error.
    /// </summary>
    /// <param name="from">Start date of the range (inclusive). Defaults to 30 days ago.</param>
    /// <param name="to">End date of the range (inclusive). Defaults to today.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Employer attendance response with records and last import timestamp.</returns>
    /// <response code="200">Records retrieved successfully (may be an empty list).</response>
    /// <response code="400">Invalid date range.</response>
    /// <response code="401">Unauthorized - invalid or missing JWT token.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet]
    [ProducesResponseType(typeof(EmployerAttendanceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EmployerAttendanceResponseDto>> GetAttendance(
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var telegramUserId = _jwtTokenService.GetUserIdFromToken(User);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var fromDate = from ?? today.AddDays(-30);
            var toDate = to ?? today;

            if (fromDate > toDate)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid Request",
                    detail: "The 'from' date must be on or before the 'to' date");
            }

            // Resolve the domain User entity to get its Guid ID
            var user = await _userRepository.GetByTelegramUserIdAsync(telegramUserId, cancellationToken);
            if (user is null)
            {
                _logger.LogWarning("Employer attendance requested for unknown Telegram user {TelegramUserId}", telegramUserId);
                return Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Authentication Failed",
                    detail: "User not found");
            }

            var records = await _employerAttendanceRepository.GetByUserAndDateRangeAsync(user.Id, fromDate, toDate, cancellationToken);
            var lastImportLog = await _employerAttendanceRepository.GetLastImportAsync(user.Id, cancellationToken);

            var recordDtos = records.Select(r => new EmployerAttendanceRecordDto
            {
                Date = r.Date,
                ClockIn = r.ClockIn,
                ClockOut = r.ClockOut,
                WorkingHours = r.WorkingHours,
                HasConflict = r.HasConflict,
                ConflictType = r.ConflictType,
                EventTypes = r.EventTypes
            }).ToList();

            return Ok(new EmployerAttendanceResponseDto
            {
                Records = recordDtos,
                LastImport = lastImportLog?.CreatedAt,
                TotalRecords = recordDtos.Count
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
            _logger.LogError(ex, "Error retrieving employer attendance records");
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An error occurred while retrieving employer attendance records");
        }
    }
}
