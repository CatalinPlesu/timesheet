using TimeSheet.Core.Application.Services;

namespace TimeSheet.Tests.Unit.Services;

/// <summary>
/// Unit tests for MnemonicService.
/// Tests BIP39 mnemonic generation.
/// Note: Storage, validation, and consumption tests are now in integration tests
/// since they require database access.
/// </summary>
public class MnemonicServiceTests
{
    [Fact]
    public void GenerateMnemonic_ReturnsValidMnemonic()
    {
        // Arrange
        var service = CreateService();

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
        var service = CreateService();

        // Act
        var mnemonic1 = service.GenerateMnemonic();
        var mnemonic2 = service.GenerateMnemonic();

        // Assert
        Assert.NotEqual(mnemonic1.ToString(), mnemonic2.ToString());
    }

    private static MnemonicService CreateService()
    {
        // MnemonicService now requires dependencies for storage operations,
        // but GenerateMnemonic doesn't use them, so we can pass nulls for unit testing
        return new MnemonicService(null!, null!);
    }
}
