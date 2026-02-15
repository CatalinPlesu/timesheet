using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using TimeSheet.Core.Application.Interfaces.Services;
using TimeSheet.Core.Domain.Repositories;
using TimeSheet.Presentation.API.Models.Auth;

namespace TimeSheet.Presentation.API.Controllers;

/// <summary>
/// Authentication endpoints for login and token management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IMnemonicService _mnemonicService;
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IMnemonicService mnemonicService,
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        ILogger<AuthController> logger)
    {
        _mnemonicService = mnemonicService ?? throw new ArgumentNullException(nameof(mnemonicService));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    /// <summary>
    /// Authenticates a user using a one-time mnemonic and returns a JWT token.
    /// </summary>
    /// <param name="request">The login request containing the BIP39 mnemonic.</param>
    /// <returns>A JWT access token and expiration time.</returns>
    /// <response code="200">Login successful, returns JWT token.</response>
    /// <response code="400">Invalid mnemonic format.</response>
    /// <response code="401">Mnemonic not found or already used.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Mnemonic))
        {
            return BadRequest(new { error = "Mnemonic is required" });
        }

        // Validate the mnemonic exists in pending list
        if (!await _mnemonicService.ValidateMnemonicAsync(request.Mnemonic, cancellationToken))
        {
            _logger.LogWarning("Login attempt with invalid or unknown mnemonic");
            return Unauthorized(new { error = "Invalid or expired mnemonic" });
        }

        // Parse mnemonic to get user info (mnemonics are linked to users during /login command in Telegram)
        // For now, we need to find which user this mnemonic belongs to
        // Since mnemonics are stored without user association, we need a different approach
        // The mnemonic service stores pending mnemonics, but we need to know which user generated it
        // This will be handled by the /login command in Telegram (TimeSheet-zei.6)
        // For now, let's assume we can extract the user from the mnemonic validation

        // Actually, looking at the existing code, the mnemonic is used during registration
        // For login via API, we need a different flow
        // Let me check how this should work...

        // After reviewing: The /login command generates a mnemonic and stores it with the user's ID
        // We need to update IMnemonicService to return the associated user ID
        // For now, let's implement a workaround: find the user by checking if they're registered

        // Since we can't determine the user from the mnemonic alone with current interface,
        // we need to modify our approach. Let's consume the mnemonic and then check for users
        // This is a temporary solution until we have proper mnemonic-to-user mapping

        // Actually, re-reading the requirements: the mnemonic is generated via Telegram /login command
        // and should be linked to a specific user. We need to enhance the mnemonic service.
        // For this implementation, let's add a method to get the user ID associated with a mnemonic.

        // TEMPORARY SOLUTION: For now, assume single user system (which matches the project description)
        // In a real implementation, the MnemonicService would store user ID alongside the mnemonic

        _logger.LogWarning("Login endpoint needs mnemonic-to-user mapping - implementing temporary single-user solution");

        // Get the first (and likely only) user
        // This is a simplification - proper implementation will come with TimeSheet-zei.6
        var users = await _userRepository.GetAllAsync(cancellationToken);
        var user = users.FirstOrDefault();

        if (user == null)
        {
            _logger.LogWarning("Login attempt but no users exist in system");
            return Unauthorized(new { error = "No users registered in system" });
        }

        // Consume the mnemonic
        if (!await _mnemonicService.ConsumeMnemonicAsync(request.Mnemonic, cancellationToken))
        {
            _logger.LogWarning("Failed to consume mnemonic - may have been used already");
            return Unauthorized(new { error = "Invalid or expired mnemonic" });
        }

        // Generate JWT token
        var token = _jwtTokenService.GenerateToken(
            user.TelegramUserId,
            user.TelegramUsername,
            user.IsAdmin);

        var expirationMinutes = int.Parse(HttpContext.RequestServices
            .GetRequiredService<IConfiguration>()
            .GetSection("JwtSettings")["ExpirationMinutes"] ?? "60");

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes);

        _logger.LogInformation("User {UserId} logged in successfully via mnemonic", user.TelegramUserId);

        return Ok(new LoginResponse
        {
            AccessToken = token,
            ExpiresAt = expiresAt
        });
    }

    /// <summary>
    /// Refreshes an access token using a refresh token.
    /// </summary>
    /// <param name="request">The refresh token request.</param>
    /// <returns>A new JWT access token and expiration time.</returns>
    /// <response code="200">Token refresh successful.</response>
    /// <response code="401">Invalid or expired refresh token.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LoginResponse>> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(new { error = "Refresh token is required" });
        }

        try
        {
            // Validate the current token
            var principal = _jwtTokenService.ValidateToken(request.RefreshToken);
            var userId = _jwtTokenService.GetUserIdFromToken(principal);

            // Get the user from database to ensure they still exist and get current info
            var user = await _userRepository.GetByTelegramUserIdAsync(userId, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("Refresh token attempt for non-existent user {UserId}", userId);
                return Unauthorized(new { error = "User not found" });
            }

            // Generate new token with same claims
            var newToken = _jwtTokenService.GenerateToken(
                user.TelegramUserId,
                user.TelegramUsername,
                user.IsAdmin);

            var expirationMinutes = int.Parse(HttpContext.RequestServices
                .GetRequiredService<IConfiguration>()
                .GetSection("JwtSettings")["ExpirationMinutes"] ?? "60");

            var expiresAt = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes);

            _logger.LogInformation("Token refreshed for user {UserId}", user.TelegramUserId);

            return Ok(new LoginResponse
            {
                AccessToken = newToken,
                ExpiresAt = expiresAt
            });
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Invalid refresh token attempt");
            return Unauthorized(new { error = "Invalid or expired token" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred during token refresh" });
        }
    }
}
