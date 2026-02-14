using Microsoft.AspNetCore.Mvc;
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
    public Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        throw new NotImplementedException("Login endpoint will be implemented in TimeSheet-zei.2");
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
    public Task<ActionResult<LoginResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        throw new NotImplementedException("Refresh token endpoint will be implemented in TimeSheet-zei.2");
    }
}
