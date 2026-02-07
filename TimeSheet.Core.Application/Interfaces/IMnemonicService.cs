using TimeSheet.Core.Domain.ValueObjects;

namespace TimeSheet.Core.Application.Interfaces;

/// <summary>
/// Service for generating and managing BIP39 mnemonics for user registration.
/// </summary>
public interface IMnemonicService
{
    /// <summary>
    /// Generates a new 24-word BIP39 mnemonic.
    /// </summary>
    /// <returns>A new RegistrationMnemonic instance.</returns>
    RegistrationMnemonic GenerateMnemonic();

    /// <summary>
    /// Stores a mnemonic as pending for registration.
    /// </summary>
    /// <param name="mnemonic">The mnemonic to store.</param>
    void StorePendingMnemonic(RegistrationMnemonic mnemonic);

    /// <summary>
    /// Validates that a mnemonic exists in the pending list and consumes it (removes it).
    /// </summary>
    /// <param name="mnemonicString">The space-separated mnemonic string to validate.</param>
    /// <returns>True if the mnemonic was found and consumed, false otherwise.</returns>
    bool ValidateAndConsumeMnemonic(string mnemonicString);

    /// <summary>
    /// Validates that a mnemonic exists in the pending list without consuming it.
    /// </summary>
    /// <param name="mnemonicString">The space-separated mnemonic string to validate.</param>
    /// <returns>True if the mnemonic is found in the pending list, false otherwise.</returns>
    bool ValidateMnemonic(string mnemonicString);

    /// <summary>
    /// Consumes (removes) a mnemonic from the pending list.
    /// </summary>
    /// <param name="mnemonicString">The space-separated mnemonic string to consume.</param>
    /// <returns>True if the mnemonic was found and removed, false otherwise.</returns>
    bool ConsumeMnemonic(string mnemonicString);
}
