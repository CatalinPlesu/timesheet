namespace TimeSheet.Presentation.API.Models.Auth;

/// <summary>
/// Response model for successful login.
/// </summary>
public sealed class LoginResponse
{
    /// <summary>
    /// Gets or sets the JWT access token.
    /// Use this token in the Authorization header for subsequent requests.
    /// </summary>
    /// <example>eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...</example>
    public required string AccessToken { get; set; }

    /// <summary>
    /// Gets or sets the token expiration time in UTC.
    /// </summary>
    public required DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the refresh token (optional, for future implementation).
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Gets or sets the user's UTC offset in minutes.
    /// Used for displaying times in the user's local timezone.
    /// Example: +60 for UTC+1, -300 for UTC-5.
    /// </summary>
    public required int UtcOffsetMinutes { get; set; }
}
