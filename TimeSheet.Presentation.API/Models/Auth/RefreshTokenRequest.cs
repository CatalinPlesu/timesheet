namespace TimeSheet.Presentation.API.Models.Auth;

/// <summary>
/// Request model for refreshing an access token.
/// </summary>
public sealed class RefreshTokenRequest
{
    /// <summary>
    /// Gets or sets the refresh token.
    /// </summary>
    public required string RefreshToken { get; set; }
}
