namespace TimeSheet.Presentation.API.Models.Auth;

/// <summary>
/// Request model for user login using one-time mnemonic.
/// </summary>
public sealed class LoginRequest
{
    /// <summary>
    /// Gets or sets the BIP39 24-word mnemonic for authentication.
    /// This is a one-time password generated from Telegram /login command.
    /// </summary>
    /// <example>abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon art</example>
    public required string Mnemonic { get; set; }
}
