using TimeSheet.Core.Domain.ValueObjects;

namespace TimeSheet.Tests.Unit.ValueObjects;

/// <summary>
/// Unit tests for RegistrationMnemonic value object.
/// Tests creation, parsing, validation, and equality.
/// </summary>
public class RegistrationMnemonicTests
{
    #region Create Tests

    [Fact]
    public void Create_ValidWords_ReturnsInstance()
    {
        // Arrange
        var words = GenerateValidWords();

        // Act
        var mnemonic = RegistrationMnemonic.Create(words);

        // Assert
        Assert.NotNull(mnemonic);
        Assert.Equal(words, mnemonic.Words);
    }

    [Fact]
    public void Create_NullWords_ThrowsArgumentException()
    {
        // Arrange
        string[] words = null!;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => RegistrationMnemonic.Create(words));
        Assert.Contains("must contain exactly 24 words", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(12)]
    [InlineData(23)]
    [InlineData(25)]
    [InlineData(48)]
    public void Create_InvalidWordCount_ThrowsArgumentException(int wordCount)
    {
        // Arrange
        var words = Enumerable.Range(0, wordCount).Select(i => $"word{i}").ToArray();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => RegistrationMnemonic.Create(words));
        Assert.Contains("must contain exactly 24 words", exception.Message);
    }

    [Fact]
    public void Create_ContainsEmptyWord_ThrowsArgumentException()
    {
        // Arrange
        var words = GenerateValidWords();
        words[10] = ""; // Make one word empty

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => RegistrationMnemonic.Create(words));
        Assert.Contains("cannot contain empty words", exception.Message);
    }

    [Fact]
    public void Create_ContainsWhitespaceWord_ThrowsArgumentException()
    {
        // Arrange
        var words = GenerateValidWords();
        words[5] = "   "; // Make one word whitespace

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => RegistrationMnemonic.Create(words));
        Assert.Contains("cannot contain empty words", exception.Message);
    }

    [Fact]
    public void Create_ContainsNullWord_ThrowsArgumentException()
    {
        // Arrange
        var words = GenerateValidWords();
        words[15] = null!; // Make one word null

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => RegistrationMnemonic.Create(words));
        Assert.Contains("cannot contain empty words", exception.Message);
    }

    #endregion

    #region Parse Tests

    [Fact]
    public void Parse_ValidMnemonicString_ReturnsInstance()
    {
        // Arrange
        var words = GenerateValidWords();
        var mnemonicString = string.Join(' ', words);

        // Act
        var mnemonic = RegistrationMnemonic.Parse(mnemonicString);

        // Assert
        Assert.NotNull(mnemonic);
        Assert.Equal(words, mnemonic.Words);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_EmptyOrWhiteSpace_ThrowsArgumentException(string mnemonicString)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            RegistrationMnemonic.Parse(mnemonicString));
        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Fact]
    public void Parse_Null_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            RegistrationMnemonic.Parse(null!));
        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Fact]
    public void Parse_TooFewWords_ThrowsArgumentException()
    {
        // Arrange
        var mnemonicString = "word1 word2 word3"; // Only 3 words

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            RegistrationMnemonic.Parse(mnemonicString));
        Assert.Contains("must contain exactly 24 words", exception.Message);
    }

    [Fact]
    public void Parse_TooManyWords_ThrowsArgumentException()
    {
        // Arrange
        var words = Enumerable.Range(0, 25).Select(i => $"word{i}"); // 25 words
        var mnemonicString = string.Join(' ', words);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            RegistrationMnemonic.Parse(mnemonicString));
        Assert.Contains("must contain exactly 24 words", exception.Message);
    }

    [Fact]
    public void Parse_ExtraWhitespace_ParsesCorrectly()
    {
        // Arrange
        var words = GenerateValidWords();
        var mnemonicString = string.Join("  ", words); // Double spaces

        // Act
        var mnemonic = RegistrationMnemonic.Parse(mnemonicString);

        // Assert
        Assert.NotNull(mnemonic);
        Assert.Equal(words, mnemonic.Words);
    }

    [Fact]
    public void Parse_LeadingAndTrailingWhitespace_ParsesCorrectly()
    {
        // Arrange
        var words = GenerateValidWords();
        var mnemonicString = "  " + string.Join(' ', words) + "  ";

        // Act
        var mnemonic = RegistrationMnemonic.Parse(mnemonicString);

        // Assert
        Assert.NotNull(mnemonic);
        Assert.Equal(words, mnemonic.Words);
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ReturnsSpaceSeparatedWords()
    {
        // Arrange
        var words = GenerateValidWords();
        var mnemonic = RegistrationMnemonic.Create(words);

        // Act
        var result = mnemonic.ToString();

        // Assert
        var expected = string.Join(' ', words);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_SameMnemonic_ReturnsTrue()
    {
        // Arrange
        var words = GenerateValidWords();
        var mnemonic1 = RegistrationMnemonic.Create(words);
        var mnemonic2 = RegistrationMnemonic.Create(words);

        // Act & Assert
        Assert.True(mnemonic1.Equals(mnemonic2));
        Assert.True(mnemonic2.Equals(mnemonic1));
    }

    [Fact]
    public void Equals_DifferentMnemonic_ReturnsFalse()
    {
        // Arrange
        var words1 = GenerateValidWords();
        var words2 = GenerateValidWords();
        words2[0] = "different"; // Make one word different

        var mnemonic1 = RegistrationMnemonic.Create(words1);
        var mnemonic2 = RegistrationMnemonic.Create(words2);

        // Act & Assert
        Assert.False(mnemonic1.Equals(mnemonic2));
        Assert.False(mnemonic2.Equals(mnemonic1));
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        // Arrange
        var words = GenerateValidWords();
        var mnemonic = RegistrationMnemonic.Create(words);

        // Act & Assert
        Assert.False(mnemonic.Equals(null));
    }

    [Fact]
    public void Equals_DifferentType_ReturnsFalse()
    {
        // Arrange
        var words = GenerateValidWords();
        var mnemonic = RegistrationMnemonic.Create(words);
        var otherObject = "not a mnemonic";

        // Act & Assert
        Assert.False(mnemonic.Equals(otherObject));
    }

    [Fact]
    public void GetHashCode_SameMnemonic_ReturnsSameHash()
    {
        // Arrange
        var words = GenerateValidWords();
        var mnemonic1 = RegistrationMnemonic.Create(words);
        var mnemonic2 = RegistrationMnemonic.Create(words);

        // Act
        var hash1 = mnemonic1.GetHashCode();
        var hash2 = mnemonic2.GetHashCode();

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void GetHashCode_DifferentMnemonic_ReturnsDifferentHash()
    {
        // Arrange
        var words1 = GenerateValidWords();
        var words2 = GenerateValidWords();
        words2[0] = "different";

        var mnemonic1 = RegistrationMnemonic.Create(words1);
        var mnemonic2 = RegistrationMnemonic.Create(words2);

        // Act
        var hash1 = mnemonic1.GetHashCode();
        var hash2 = mnemonic2.GetHashCode();

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    #endregion

    #region Helper Methods

    private static string[] GenerateValidWords()
    {
        return Enumerable.Range(0, 24).Select(i => $"word{i}").ToArray();
    }

    #endregion
}
