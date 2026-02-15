using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TimeSheet.Core.Application.Interfaces.Persistence;
using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.Repositories;
using TimeSheet.Presentation.API.Models.Holidays;

namespace TimeSheet.Presentation.API.Controllers;

/// <summary>
/// Holiday and vacation day management endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class HolidaysController : ControllerBase
{
    private readonly IHolidayRepository _holidayRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<HolidaysController> _logger;

    public HolidaysController(
        IHolidayRepository holidayRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ILogger<HolidaysController> logger)
    {
        _holidayRepository = holidayRepository ?? throw new ArgumentNullException(nameof(holidayRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all holidays for the authenticated user.
    /// </summary>
    /// <returns>A list of all holidays for the user.</returns>
    /// <response code="200">Holidays retrieved successfully.</response>
    /// <response code="401">Unauthorized - invalid or missing JWT token.</response>
    /// <response code="404">User not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet]
    [ProducesResponseType(typeof(HolidayListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<HolidayListResponse>> GetHolidays(CancellationToken cancellationToken = default)
    {
        try
        {
            var telegramUserId = GetUserIdFromClaims();
            _logger.LogInformation("Getting holidays for Telegram user {TelegramUserId}", telegramUserId);

            var user = await _userRepository.GetByTelegramUserIdAsync(telegramUserId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("User with Telegram ID {TelegramUserId} not found", telegramUserId);
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found",
                    detail: "User not found");
            }

            var holidays = await _holidayRepository.GetByUserIdAsync(user.Id, cancellationToken);

            var response = new HolidayListResponse
            {
                Holidays = holidays.Select(MapToDto).ToList(),
                TotalCount = holidays.Count
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting holidays for user");
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An error occurred while retrieving holidays");
        }
    }

    /// <summary>
    /// Gets upcoming holidays for the authenticated user.
    /// </summary>
    /// <param name="limit">Maximum number of upcoming holidays to return (default: 10).</param>
    /// <returns>A list of upcoming holidays.</returns>
    /// <response code="200">Upcoming holidays retrieved successfully.</response>
    /// <response code="401">Unauthorized - invalid or missing JWT token.</response>
    /// <response code="404">User not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("upcoming")]
    [ProducesResponseType(typeof(HolidayListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<HolidayListResponse>> GetUpcomingHolidays(
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var telegramUserId = GetUserIdFromClaims();
            _logger.LogInformation("Getting upcoming holidays for Telegram user {TelegramUserId}", telegramUserId);

            var user = await _userRepository.GetByTelegramUserIdAsync(telegramUserId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("User with Telegram ID {TelegramUserId} not found", telegramUserId);
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found",
                    detail: "User not found");
            }

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var holidays = await _holidayRepository.GetUpcomingHolidaysAsync(user.Id, today, limit, cancellationToken);

            var response = new HolidayListResponse
            {
                Holidays = holidays.Select(MapToDto).ToList(),
                TotalCount = holidays.Count
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting upcoming holidays for user");
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An error occurred while retrieving upcoming holidays");
        }
    }

    /// <summary>
    /// Creates a new holiday for the authenticated user.
    /// </summary>
    /// <param name="request">The holiday creation request.</param>
    /// <returns>The created holiday.</returns>
    /// <response code="201">Holiday created successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="401">Unauthorized - invalid or missing JWT token.</response>
    /// <response code="404">User not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost]
    [ProducesResponseType(typeof(HolidayDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<HolidayDto>> CreateHoliday(
        [FromBody] CreateHolidayRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var telegramUserId = GetUserIdFromClaims();
            _logger.LogInformation("Creating holiday for Telegram user {TelegramUserId}", telegramUserId);

            // Validate date range
            if (request.EndDate < request.StartDate)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid Request",
                    detail: "End date cannot be before start date");
            }

            var user = await _userRepository.GetByTelegramUserIdAsync(telegramUserId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("User with Telegram ID {TelegramUserId} not found", telegramUserId);
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found",
                    detail: "User not found");
            }

            // Create the holiday entity
            var holiday = new Holiday(
                user.Id,
                request.StartDate,
                request.EndDate,
                request.Type,
                request.Description);

            // Save to repository
            await _holidayRepository.AddAsync(holiday, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation(
                "Created holiday {HolidayId} for user {UserId} from {StartDate} to {EndDate}",
                holiday.Id,
                user.Id,
                request.StartDate,
                request.EndDate);

            var dto = MapToDto(holiday);
            return CreatedAtAction(nameof(GetHoliday), new { id = holiday.Id }, dto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid holiday creation request");
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating holiday");
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An error occurred while creating the holiday");
        }
    }

    /// <summary>
    /// Gets a specific holiday by ID.
    /// </summary>
    /// <param name="id">The holiday ID.</param>
    /// <returns>The holiday details.</returns>
    /// <response code="200">Holiday retrieved successfully.</response>
    /// <response code="401">Unauthorized - invalid or missing JWT token.</response>
    /// <response code="404">Holiday not found or belongs to another user.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(HolidayDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<HolidayDto>> GetHoliday(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var telegramUserId = GetUserIdFromClaims();
            var user = await _userRepository.GetByTelegramUserIdAsync(telegramUserId, cancellationToken);
            if (user == null)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found",
                    detail: "User not found");
            }

            var holiday = await _holidayRepository.GetByIdAsync(id, cancellationToken);
            if (holiday == null || holiday.UserId != user.Id)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found",
                    detail: "Holiday not found");
            }

            return Ok(MapToDto(holiday));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting holiday {HolidayId}", id);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An error occurred while retrieving the holiday");
        }
    }

    /// <summary>
    /// Deletes a holiday by ID.
    /// </summary>
    /// <param name="id">The holiday ID to delete.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Holiday deleted successfully.</response>
    /// <response code="401">Unauthorized - invalid or missing JWT token.</response>
    /// <response code="404">Holiday not found or belongs to another user.</response>
    /// <response code="500">Internal server error.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteHoliday(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var telegramUserId = GetUserIdFromClaims();
            _logger.LogInformation("Deleting holiday {HolidayId} for Telegram user {TelegramUserId}", id, telegramUserId);

            var user = await _userRepository.GetByTelegramUserIdAsync(telegramUserId, cancellationToken);
            if (user == null)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found",
                    detail: "User not found");
            }

            var holiday = await _holidayRepository.GetByIdAsync(id, cancellationToken);
            if (holiday == null || holiday.UserId != user.Id)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found",
                    detail: "Holiday not found");
            }

            _holidayRepository.Remove(holiday);
            await _unitOfWork.CompleteAsync(cancellationToken);

            _logger.LogInformation("Deleted holiday {HolidayId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting holiday {HolidayId}", id);
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An error occurred while deleting the holiday");
        }
    }

    /// <summary>
    /// Maps a Holiday entity to a HolidayDto.
    /// </summary>
    private static HolidayDto MapToDto(Holiday holiday)
    {
        return new HolidayDto
        {
            Id = holiday.Id,
            StartDate = holiday.StartDate,
            EndDate = holiday.EndDate,
            Type = holiday.Type,
            Description = holiday.Description,
            IsSingleDay = holiday.IsSingleDay,
            DayCount = holiday.DayCount,
            CreatedAt = holiday.CreatedAt
        };
    }

    /// <summary>
    /// Extracts the Telegram user ID from JWT claims.
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
}
