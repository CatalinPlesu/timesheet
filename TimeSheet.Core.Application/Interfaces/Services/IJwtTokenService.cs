using System.Security.Claims;

namespace TimeSheet.Core.Application.Interfaces.Services;

/// <summary>
/// Service for generating and validating JWT tokens.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a JWT token for a user.
    /// </summary>
    /// <param name="userId">The Telegram user ID.</param>
    /// <param name="username">The Telegram username (optional).</param>
    /// <param name="isAdmin">Whether the user is an administrator.</param>
    /// <returns>A JWT token string.</returns>
    string GenerateToken(long userId, string? username, bool isAdmin);

    /// <summary>
    /// Validates a JWT token and returns the claims principal.
    /// </summary>
    /// <param name="token">The JWT token to validate.</param>
    /// <returns>The claims principal if the token is valid.</returns>
    /// <exception cref="SecurityTokenException">Thrown when the token is invalid.</exception>
    ClaimsPrincipal ValidateToken(string token);

    /// <summary>
    /// Extracts the user ID from a claims principal.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The Telegram user ID.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the user ID claim is not found.</exception>
    long GetUserIdFromToken(ClaimsPrincipal principal);
}
