using System.Collections.Concurrent;
using NBitcoin;
using TimeSheet.Core.Application.Interfaces.Services;
using TimeSheet.Core.Domain.ValueObjects;

namespace TimeSheet.Core.Application.Services;

/// <summary>
/// Implementation of IMnemonicService using NBitcoin for BIP39 mnemonic generation.
/// </summary>
public sealed class MnemonicService : IMnemonicService
{
    private readonly ConcurrentDictionary<string, byte> _pendingMnemonics = new();

    /// <inheritdoc/>
    public RegistrationMnemonic GenerateMnemonic()
    {
        var mnemonic = new Mnemonic(Wordlist.English, WordCount.TwentyFour);
        var words = mnemonic.Words;
        return RegistrationMnemonic.Create(words);
    }

    /// <inheritdoc/>
    public void StorePendingMnemonic(RegistrationMnemonic mnemonic)
    {
        ArgumentNullException.ThrowIfNull(mnemonic);
        _pendingMnemonics.TryAdd(mnemonic.ToString(), 0);
    }

    /// <inheritdoc/>
    public bool ValidateAndConsumeMnemonic(string mnemonicString)
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

        // Atomically remove the mnemonic if it exists
        return _pendingMnemonics.TryRemove(normalizedMnemonic, out _);
    }

    /// <inheritdoc/>
    public bool ValidateMnemonic(string mnemonicString)
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

        // Check if the mnemonic exists in the pending list
        return _pendingMnemonics.ContainsKey(normalizedMnemonic);
    }

    /// <inheritdoc/>
    public bool ConsumeMnemonic(string mnemonicString)
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

        // Atomically remove the mnemonic if it exists
        return _pendingMnemonics.TryRemove(normalizedMnemonic, out _);
    }
}
