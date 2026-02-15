using TimeSheet.Core.Domain.ValueObjects;

namespace TimeSheet.Core.Application.Interfaces.Services;

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
    /// The mnemonic will expire after 15 minutes.
    /// </summary>
    /// <param name="mnemonic">The mnemonic to store.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    Task StorePendingMnemonicAsync(RegistrationMnemonic mnemonic, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that a mnemonic exists in the pending list and consumes it (removes it).
    /// </summary>
    /// <param name="mnemonicString">The space-separated mnemonic string to validate.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>True if the mnemonic was found and consumed, false otherwise.</returns>
    Task<bool> ValidateAndConsumeMnemonicAsync(string mnemonicString, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that a mnemonic exists in the pending list without consuming it.
    /// </summary>
    /// <param name="mnemonicString">The space-separated mnemonic string to validate.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>True if the mnemonic is found in the pending list, false otherwise.</returns>
    Task<bool> ValidateMnemonicAsync(string mnemonicString, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consumes (removes) a mnemonic from the pending list.
    /// </summary>
    /// <param name="mnemonicString">The space-separated mnemonic string to consume.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>True if the mnemonic was found and removed, false otherwise.</returns>
    Task<bool> ConsumeMnemonicAsync(string mnemonicString, CancellationToken cancellationToken = default);
}
