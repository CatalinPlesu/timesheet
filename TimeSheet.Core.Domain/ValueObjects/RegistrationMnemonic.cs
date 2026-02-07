namespace TimeSheet.Core.Domain.ValueObjects;

/// <summary>
/// Represents a BIP39 mnemonic phrase used for user registration.
/// </summary>
public sealed record RegistrationMnemonic
{
    private const int RequiredWordCount = 24;

    /// <summary>
    /// Gets the 24 words comprising the mnemonic.
    /// </summary>
    public required string[] Words { get; init; }

    /// <summary>
    /// Creates a new RegistrationMnemonic from an array of words.
    /// </summary>
    /// <param name="words">The 24 words.</param>
    /// <returns>A RegistrationMnemonic instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the word count is not 24.</exception>
    public static RegistrationMnemonic Create(string[] words)
    {
        if (words == null || words.Length != RequiredWordCount)
        {
            throw new ArgumentException($"Mnemonic must contain exactly {RequiredWordCount} words.", nameof(words));
        }

        // Validate no empty words
        if (words.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Mnemonic cannot contain empty words.", nameof(words));
        }

        return new RegistrationMnemonic { Words = words };
    }

    /// <summary>
    /// Parses a space-separated mnemonic string.
    /// </summary>
    /// <param name="mnemonicString">The space-separated mnemonic phrase.</param>
    /// <returns>A RegistrationMnemonic instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the word count is not 24.</exception>
    public static RegistrationMnemonic Parse(string mnemonicString)
    {
        if (string.IsNullOrWhiteSpace(mnemonicString))
        {
            throw new ArgumentException("Mnemonic string cannot be null or empty.", nameof(mnemonicString));
        }

        var words = mnemonicString.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return Create(words);
    }

    /// <summary>
    /// Returns the mnemonic as a space-separated string.
    /// </summary>
    public override string ToString()
    {
        return string.Join(' ', Words);
    }
}
