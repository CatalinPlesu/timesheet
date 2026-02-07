using TimeSheet.Core.Application.Services;
using TimeSheet.Core.Domain.ValueObjects;

namespace TimeSheet.Tests.Unit.Services;

/// <summary>
/// Unit tests for MnemonicService.
/// Tests BIP39 mnemonic generation, storage, and validation.
/// </summary>
public class MnemonicServiceTests
{
    [Fact]
    public void GenerateMnemonic_ReturnsValidMnemonic()
    {
        // Arrange
        var service = new MnemonicService();

        // Act
        var mnemonic = service.GenerateMnemonic();

        // Assert
        Assert.NotNull(mnemonic);
        Assert.NotNull(mnemonic.Words);
        Assert.Equal(24, mnemonic.Words.Length);
        Assert.All(mnemonic.Words, word => Assert.False(string.IsNullOrWhiteSpace(word)));
    }

    [Fact]
    public void GenerateMnemonic_MultipleCalls_ReturnsDifferentMnemonics()
    {
        // Arrange
        var service = new MnemonicService();

        // Act
        var mnemonic1 = service.GenerateMnemonic();
        var mnemonic2 = service.GenerateMnemonic();

        // Assert
        Assert.NotEqual(mnemonic1.ToString(), mnemonic2.ToString());
    }

    [Fact]
    public void StorePendingMnemonic_NullMnemonic_ThrowsArgumentNullException()
    {
        // Arrange
        var service = new MnemonicService();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.StorePendingMnemonic(null!));
    }

    [Fact]
    public void StorePendingMnemonic_ValidMnemonic_Stores()
    {
        // Arrange
        var service = new MnemonicService();
        var mnemonic = service.GenerateMnemonic();

        // Act
        service.StorePendingMnemonic(mnemonic);

        // Assert
        // Validate by consuming it
        Assert.True(service.ValidateAndConsumeMnemonic(mnemonic.ToString()));
    }

    [Fact]
    public void ValidateAndConsumeMnemonic_ValidMnemonic_ReturnsTrue()
    {
        // Arrange
        var service = new MnemonicService();
        var mnemonic = service.GenerateMnemonic();
        service.StorePendingMnemonic(mnemonic);

        // Act
        var result = service.ValidateAndConsumeMnemonic(mnemonic.ToString());

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateAndConsumeMnemonic_ValidMnemonic_RemovesFromPending()
    {
        // Arrange
        var service = new MnemonicService();
        var mnemonic = service.GenerateMnemonic();
        service.StorePendingMnemonic(mnemonic);

        // Act
        var firstAttempt = service.ValidateAndConsumeMnemonic(mnemonic.ToString());
        var secondAttempt = service.ValidateAndConsumeMnemonic(mnemonic.ToString());

        // Assert
        Assert.True(firstAttempt);
        Assert.False(secondAttempt); // Should not be valid the second time
    }

    [Fact]
    public void ValidateAndConsumeMnemonic_UnknownMnemonic_ReturnsFalse()
    {
        // Arrange
        var service = new MnemonicService();
        var mnemonic = service.GenerateMnemonic();
        // Note: Not storing it

        // Act
        var result = service.ValidateAndConsumeMnemonic(mnemonic.ToString());

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateAndConsumeMnemonic_EmptyOrWhiteSpace_ReturnsFalse(string mnemonicString)
    {
        // Arrange
        var service = new MnemonicService();

        // Act
        var result = service.ValidateAndConsumeMnemonic(mnemonicString);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateAndConsumeMnemonic_Null_ReturnsFalse()
    {
        // Arrange
        var service = new MnemonicService();

        // Act
        var result = service.ValidateAndConsumeMnemonic(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateAndConsumeMnemonic_InvalidMnemonicFormat_ReturnsFalse()
    {
        // Arrange
        var service = new MnemonicService();
        var invalidMnemonic = "invalid mnemonic phrase"; // Only 3 words, needs 24

        // Act
        var result = service.ValidateAndConsumeMnemonic(invalidMnemonic);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateAndConsumeMnemonic_ExtraWhitespace_NormalizesAndMatches()
    {
        // Arrange
        var service = new MnemonicService();
        var mnemonic = service.GenerateMnemonic();
        service.StorePendingMnemonic(mnemonic);

        // Add extra whitespace to the mnemonic string
        var mnemonicWithExtraSpaces = string.Join("  ", mnemonic.Words); // Double spaces

        // Act
        var result = service.ValidateAndConsumeMnemonic(mnemonicWithExtraSpaces);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateAndConsumeMnemonic_MultiplePendingMnemonics_ConsumesOnlyMatchingOne()
    {
        // Arrange
        var service = new MnemonicService();
        var mnemonic1 = service.GenerateMnemonic();
        var mnemonic2 = service.GenerateMnemonic();
        var mnemonic3 = service.GenerateMnemonic();

        service.StorePendingMnemonic(mnemonic1);
        service.StorePendingMnemonic(mnemonic2);
        service.StorePendingMnemonic(mnemonic3);

        // Act
        var result = service.ValidateAndConsumeMnemonic(mnemonic2.ToString());

        // Assert
        Assert.True(result);

        // Verify the other mnemonics are still valid
        Assert.True(service.ValidateAndConsumeMnemonic(mnemonic1.ToString()));
        Assert.True(service.ValidateAndConsumeMnemonic(mnemonic3.ToString()));

        // Verify mnemonic2 was consumed
        Assert.False(service.ValidateAndConsumeMnemonic(mnemonic2.ToString()));
    }

    [Fact]
    public void StorePendingMnemonic_SameMnemonicTwice_StoresBothInstances()
    {
        // Arrange
        var service = new MnemonicService();
        var mnemonic = service.GenerateMnemonic();

        // Act
        service.StorePendingMnemonic(mnemonic);
        service.StorePendingMnemonic(mnemonic); // Store the same mnemonic again

        // Assert
        // Should be able to consume it twice
        Assert.True(service.ValidateAndConsumeMnemonic(mnemonic.ToString()));
        Assert.True(service.ValidateAndConsumeMnemonic(mnemonic.ToString()));
        Assert.False(service.ValidateAndConsumeMnemonic(mnemonic.ToString())); // Third time should fail
    }
}
