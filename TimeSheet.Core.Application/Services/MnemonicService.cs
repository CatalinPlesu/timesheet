using System.Collections.Concurrent;
using NBitcoin;
using TimeSheet.Core.Application.Interfaces;
using TimeSheet.Core.Domain.ValueObjects;

namespace TimeSheet.Core.Application.Services;

/// <summary>
/// Implementation of IMnemonicService using NBitcoin for BIP39 mnemonic generation.
/// </summary>
public sealed class MnemonicService : IMnemonicService
{
    private readonly ConcurrentBag<string> _pendingMnemonics = new();

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
        _pendingMnemonics.Add(mnemonic.ToString());
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

        // Try to find and remove the mnemonic from pending list
        // Since ConcurrentBag doesn't support atomic "find and remove",
        // we need to rebuild the collection
        var found = false;
        List<string> tempList = [];

        foreach (var pending in _pendingMnemonics)
        {
            if (!found && pending == normalizedMnemonic)
            {
                found = true;
                // Skip this one (consume it)
            }
            else
            {
                tempList.Add(pending);
            }
        }

        if (found)
        {
            // Rebuild the concurrent bag without the consumed mnemonic
            _pendingMnemonics.Clear();
            foreach (var item in tempList)
            {
                _pendingMnemonics.Add(item);
            }
        }

        return found;
    }
}
