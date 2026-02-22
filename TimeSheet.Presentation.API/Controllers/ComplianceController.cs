using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeSheet.Core.Application.Interfaces.Services;
using TimeSheet.Core.Domain.Repositories;
using TimeSheet.Presentation.API.Models.Compliance;

namespace TimeSheet.Presentation.API.Controllers;

/// <summary>
/// Compliance endpoints for evaluating employer attendance rules against tracked time data.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class ComplianceController : ControllerBase
{
    private readonly IComplianceRuleEngine _complianceRuleEngine;
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<ComplianceController> _logger;

    public ComplianceController(
        IComplianceRuleEngine complianceRuleEngine,
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        ILogger<ComplianceController> logger)
    {
        _complianceRuleEngine = complianceRuleEngine ?? throw new ArgumentNullException(nameof(complianceRuleEngine));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets compliance violations for the authenticated user within a date range.
    /// Evaluates all enabled compliance rules (e.g., MinimumSpan) against tracked time data.
    /// Returns an empty violations list when no rules are configured — not an error.
    /// </summary>
    /// <param name="from">Start date of the range (inclusive). Defaults to 30 days ago.</param>
    /// <param name="to">End date of the range (inclusive). Defaults to today.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Compliance violations response with violation details and summary counts.</returns>
    /// <response code="200">Violations evaluated successfully (may be an empty list).</response>
    /// <response code="400">Invalid date range.</response>
    /// <response code="401">Unauthorized - invalid or missing JWT token.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("violations")]
    [ProducesResponseType(typeof(ComplianceViolationsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ComplianceViolationsResponseDto>> GetViolations(
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

            // Resolve the domain User entity to get its Guid ID required by IComplianceRuleEngine
            var user = await _userRepository.GetByTelegramUserIdAsync(telegramUserId, cancellationToken);
            if (user is null)
            {
                _logger.LogWarning("Compliance violations requested for unknown Telegram user {TelegramUserId}", telegramUserId);
                return Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Authentication Failed",
                    detail: "User not found");
            }

            var violations = await _complianceRuleEngine.EvaluateAsync(user.Id, fromDate, toDate, cancellationToken);

            var totalDays = toDate.DayNumber - fromDate.DayNumber + 1;

            var violationDtos = violations.Select(v => new ComplianceViolationDto
            {
                Date = v.Date,
                RuleType = v.RuleType,
                ActualHours = v.ActualHours,
                ThresholdHours = v.ThresholdHours,
                Description = v.Description
            }).ToList();

            return Ok(new ComplianceViolationsResponseDto
            {
                Violations = violationDtos,
                TotalDays = totalDays,
                ViolationCount = violationDtos.Count
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
            _logger.LogError(ex, "Error evaluating compliance violations");
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An error occurred while evaluating compliance violations");
        }
    }
}
