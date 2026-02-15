namespace TimeSheet.Core.Domain.Entities;

using TimeSheet.Core.Domain.SharedKernel;

/// <summary>
/// Represents a pending registration mnemonic that has been generated but not yet consumed.
/// Mnemonics are used for one-time user authentication during the login flow.
/// </summary>
public sealed class PendingMnemonic : CreatedEntity
{
    /// <summary>
    /// Gets the BIP39 mnemonic string (24 words separated by spaces).
    /// </summary>
    public string Mnemonic { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when this mnemonic expires.
    /// Mnemonics are typically valid for 15 minutes.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; init; }

    /// <summary>
    /// Gets a value indicating whether this mnemonic has been consumed (used for registration/login).
    /// Once consumed, a mnemonic cannot be reused.
    /// </summary>
    public bool IsConsumed { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PendingMnemonic"/> class.
    /// Used when creating a new pending mnemonic.
    /// </summary>
    /// <param name="mnemonic">The BIP39 mnemonic string.</param>
    /// <param name="expiresAt">The UTC timestamp when this mnemonic expires.</param>
    public PendingMnemonic(string mnemonic, DateTimeOffset expiresAt)
        : base()
    {
        if (string.IsNullOrWhiteSpace(mnemonic))
            throw new ArgumentException("Mnemonic cannot be null or whitespace.", nameof(mnemonic));

        if (expiresAt <= DateTimeOffset.UtcNow)
            throw new ArgumentException("Expiration time must be in the future.", nameof(expiresAt));

        Mnemonic = mnemonic;
        ExpiresAt = expiresAt;
        IsConsumed = false;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PendingMnemonic"/> class.
    /// Used for entity rehydration from persistence.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="createdAt">The UTC timestamp when this entity was created.</param>
    /// <param name="mnemonic">The BIP39 mnemonic string.</param>
    /// <param name="expiresAt">The UTC timestamp when this mnemonic expires.</param>
    /// <param name="isConsumed">Whether this mnemonic has been consumed.</param>
    public PendingMnemonic(
        Guid id,
        DateTimeOffset createdAt,
        string mnemonic,
        DateTimeOffset expiresAt,
        bool isConsumed)
        : base(id, createdAt)
    {
        Mnemonic = mnemonic;
        ExpiresAt = expiresAt;
        IsConsumed = isConsumed;
    }

    /// <summary>
    /// Marks this mnemonic as consumed.
    /// This operation is idempotent - consuming an already-consumed mnemonic is safe.
    /// </summary>
    public void MarkAsConsumed()
    {
        IsConsumed = true;
    }

    /// <summary>
    /// Checks if this mnemonic is currently valid (not expired and not consumed).
    /// </summary>
    /// <returns>True if the mnemonic is valid; otherwise, false.</returns>
    public bool IsValid()
    {
        return !IsConsumed && ExpiresAt > DateTimeOffset.UtcNow;
    }
}
