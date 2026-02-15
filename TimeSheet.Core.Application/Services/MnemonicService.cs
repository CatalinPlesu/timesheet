using NBitcoin;
using TimeSheet.Core.Application.Interfaces.Persistence;
using TimeSheet.Core.Application.Interfaces.Services;
using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.Repositories;
using TimeSheet.Core.Domain.ValueObjects;

namespace TimeSheet.Core.Application.Services;

/// <summary>
/// Implementation of IMnemonicService using NBitcoin for BIP39 mnemonic generation
/// and database persistence for mnemonic storage.
/// </summary>
/// <param name="pendingMnemonicRepository">Repository for managing pending mnemonics.</param>
/// <param name="unitOfWork">Unit of work for coordinating database transactions.</param>
public sealed class MnemonicService(
    IPendingMnemonicRepository pendingMnemonicRepository,
    IUnitOfWork unitOfWork) : IMnemonicService
{
    private static readonly TimeSpan MnemonicExpirationTime = TimeSpan.FromMinutes(15);

    /// <inheritdoc/>
    public RegistrationMnemonic GenerateMnemonic()
    {
        var mnemonic = new Mnemonic(Wordlist.English, WordCount.TwentyFour);
        var words = mnemonic.Words;
        return RegistrationMnemonic.Create(words);
    }

    /// <inheritdoc/>
    public async Task StorePendingMnemonicAsync(RegistrationMnemonic mnemonic, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mnemonic);

        var expiresAt = DateTimeOffset.UtcNow.Add(MnemonicExpirationTime);
        var pendingMnemonic = new PendingMnemonic(mnemonic.ToString(), expiresAt);

        await pendingMnemonicRepository.AddAsync(pendingMnemonic, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateAndConsumeMnemonicAsync(string mnemonicString, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mnemonicString))
        {
            return false;
        }

        // Normalize the mnemonic string by parsing and converting back to string
        // This ensures consistent formatting for comparison
        RegistrationMnemonic parsedMnemonic;
        try
        {
            parsedMnemonic = RegistrationMnemonic.Parse(mnemonicString);
        }
        catch (ArgumentException)
        {
            // Invalid mnemonic format
            return false;
        }

        var normalizedMnemonic = parsedMnemonic.ToString();

        // Find the mnemonic in the database
        var pendingMnemonic = await pendingMnemonicRepository.FindByMnemonicAsync(normalizedMnemonic, cancellationToken);

        // Check if mnemonic exists and is valid (not consumed, not expired)
        if (pendingMnemonic == null || !pendingMnemonic.IsValid())
        {
            return false;
        }

        // Mark as consumed and save
        pendingMnemonic.MarkAsConsumed();
        pendingMnemonicRepository.Update(pendingMnemonic);
        await unitOfWork.CompleteAsync(cancellationToken);

        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateMnemonicAsync(string mnemonicString, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mnemonicString))
        {
            return false;
        }

        // Normalize the mnemonic string
        RegistrationMnemonic parsedMnemonic;
        try
        {
            parsedMnemonic = RegistrationMnemonic.Parse(mnemonicString);
        }
        catch (ArgumentException)
        {
            // Invalid mnemonic format
            return false;
        }

        var normalizedMnemonic = parsedMnemonic.ToString();

        // Find the mnemonic in the database
        var pendingMnemonic = await pendingMnemonicRepository.FindByMnemonicAsync(normalizedMnemonic, cancellationToken);

        // Check if mnemonic exists and is valid (not consumed, not expired)
        return pendingMnemonic != null && pendingMnemonic.IsValid();
    }

    /// <inheritdoc/>
    public async Task<bool> ConsumeMnemonicAsync(string mnemonicString, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mnemonicString))
        {
            return false;
        }

        // Normalize the mnemonic string
        RegistrationMnemonic parsedMnemonic;
        try
        {
            parsedMnemonic = RegistrationMnemonic.Parse(mnemonicString);
        }
        catch (ArgumentException)
        {
            // Invalid mnemonic format
            return false;
        }

        var normalizedMnemonic = parsedMnemonic.ToString();

        // Find the mnemonic in the database
        var pendingMnemonic = await pendingMnemonicRepository.FindByMnemonicAsync(normalizedMnemonic, cancellationToken);

        // Check if mnemonic exists and is valid
        if (pendingMnemonic == null || !pendingMnemonic.IsValid())
        {
            return false;
        }

        // Mark as consumed and save
        pendingMnemonic.MarkAsConsumed();
        pendingMnemonicRepository.Update(pendingMnemonic);
        await unitOfWork.CompleteAsync(cancellationToken);

        return true;
    }
}
