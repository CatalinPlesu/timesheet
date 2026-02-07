using TimeSheet.Core.Application.Parsers;

namespace TimeSheet.Tests.Unit.Parsers;

/// <summary>
/// Unit tests for CommandParameterParser.
/// Tests time parsing logic with various input formats.
/// </summary>
public class CommandParameterParserTests
{
    private readonly CommandParameterParser _parser = new();

    #region No Parameter Tests

    [Fact]
    public void ParseTimestamp_NoParameter_ReturnsCurrentTime()
    {
        // Arrange
        var commandText = "/work";
        var utcOffsetMinutes = 0;
        var before = DateTime.UtcNow;

        // Act
        var result = _parser.ParseTimestamp(commandText, utcOffsetMinutes);

        // Assert
        var after = DateTime.UtcNow;
        Assert.InRange(result, before.AddSeconds(-1), after.AddSeconds(1));
    }

    #endregion

    #region Minute Offset Tests

    [Theory]
    [InlineData("/work -15", -15)]
    [InlineData("/work -30", -30)]
    [InlineData("/work -m 15", -15)]
    [InlineData("/work -m 30", -30)]
    [InlineData("/work -m15", -15)]
    [InlineData("/work -M 15", -15)] // Case insensitive
    public void ParseTimestamp_NegativeMinuteOffset_SubtractsMinutes(string commandText, int expectedOffsetMinutes)
    {
        // Arrange
        var utcOffsetMinutes = 0;
        var now = DateTime.UtcNow;

        // Act
        var result = _parser.ParseTimestamp(commandText, utcOffsetMinutes);

        // Assert
        var expected = now.AddMinutes(expectedOffsetMinutes);
        Assert.InRange(result, expected.AddSeconds(-1), expected.AddSeconds(1));
    }

    [Theory]
    [InlineData("/work +15", 15)]
    [InlineData("/work +30", 30)]
    [InlineData("/work +m 15", 15)]
    [InlineData("/work +m 30", 30)]
    [InlineData("/work +m15", 15)]
    [InlineData("/work +M 15", 15)] // Case insensitive
    public void ParseTimestamp_PositiveMinuteOffset_AddsMinutes(string commandText, int expectedOffsetMinutes)
    {
        // Arrange
        var utcOffsetMinutes = 0;
        var now = DateTime.UtcNow;

        // Act
        var result = _parser.ParseTimestamp(commandText, utcOffsetMinutes);

        // Assert
        var expected = now.AddMinutes(expectedOffsetMinutes);
        Assert.InRange(result, expected.AddSeconds(-1), expected.AddSeconds(1));
    }

    [Fact]
    public void ParseTimestamp_MaxMinuteOffset_Succeeds()
    {
        // Arrange
        var commandText = "/work -720"; // 12 hours
        var utcOffsetMinutes = 0;

        // Act
        var result = _parser.ParseTimestamp(commandText, utcOffsetMinutes);

        // Assert
        var expected = DateTime.UtcNow.AddMinutes(-720);
        Assert.InRange(result, expected.AddSeconds(-1), expected.AddSeconds(1));
    }

    [Fact]
    public void ParseTimestamp_ExceedsMaxMinuteOffset_ThrowsArgumentException()
    {
        // Arrange
        var commandText = "/work -721"; // Exceeds 12 hours
        var utcOffsetMinutes = 0;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _parser.ParseTimestamp(commandText, utcOffsetMinutes));
        Assert.Contains("Minute offset too large", exception.Message);
        Assert.Contains("721", exception.Message);
    }

    #endregion

    #region Explicit Time Tests

    [Theory]
    [InlineData("/work 09:00", 9, 0)]
    [InlineData("/work 14:30", 14, 30)]
    [InlineData("/work 23:59", 23, 59)]
    [InlineData("/work 00:00", 0, 0)]
    [InlineData("/work [09:00]", 9, 0)]
    [InlineData("/work [14:30]", 14, 30)]
    public void ParseTimestamp_ExplicitTime_ReturnsCorrectUtcTime(string commandText, int hour, int minute)
    {
        // Arrange
        var utcOffsetMinutes = 120; // UTC+2
        var now = DateTime.UtcNow;
        var userLocalNow = now.AddMinutes(utcOffsetMinutes);

        // Act
        var result = _parser.ParseTimestamp(commandText, utcOffsetMinutes);

        // Assert
        // Expected time in user's local time
        var expectedUserLocalTime = new DateTime(
            userLocalNow.Year,
            userLocalNow.Month,
            userLocalNow.Day,
            hour,
            minute,
            0,
            DateTimeKind.Unspecified);

        // Convert to UTC
        var expectedUtc = expectedUserLocalTime.AddMinutes(-utcOffsetMinutes);

        Assert.Equal(expectedUtc, result);
    }

    [Theory]
    [InlineData("/work 24:00")] // Invalid hour
    [InlineData("/work 25:30")] // Invalid hour
    [InlineData("/work 14:60")] // Invalid minute
    [InlineData("/work 14:99")] // Invalid minute
    public void ParseTimestamp_InvalidTimeComponents_ThrowsArgumentException(string commandText)
    {
        // Arrange
        var utcOffsetMinutes = 0;

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _parser.ParseTimestamp(commandText, utcOffsetMinutes));
    }

    [Fact]
    public void ParseTimestamp_ExplicitTimeWithNegativeUtcOffset_ConvertsCorrectly()
    {
        // Arrange
        var commandText = "/work 14:00";
        var utcOffsetMinutes = -300; // UTC-5 (e.g., EST)
        var now = DateTime.UtcNow;
        var userLocalNow = now.AddMinutes(utcOffsetMinutes);

        // Act
        var result = _parser.ParseTimestamp(commandText, utcOffsetMinutes);

        // Assert
        var expectedUserLocalTime = new DateTime(
            userLocalNow.Year,
            userLocalNow.Month,
            userLocalNow.Day,
            14,
            0,
            0,
            DateTimeKind.Unspecified);

        var expectedUtc = expectedUserLocalTime.AddMinutes(-utcOffsetMinutes);

        Assert.Equal(expectedUtc, result);
    }

    #endregion

    #region Edge Cases and Error Handling

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void ParseTimestamp_EmptyOrWhiteSpaceCommand_ThrowsArgumentException(string commandText)
    {
        // Arrange
        var utcOffsetMinutes = 0;

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _parser.ParseTimestamp(commandText, utcOffsetMinutes));
    }

    [Fact]
    public void ParseTimestamp_NullCommand_ThrowsArgumentNullException()
    {
        // Arrange
        var utcOffsetMinutes = 0;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _parser.ParseTimestamp(null!, utcOffsetMinutes));
    }

    [Theory]
    [InlineData("/work abc")]
    [InlineData("/work invalid")]
    [InlineData("/work -")]
    [InlineData("/work +")]
    public void ParseTimestamp_InvalidParameterFormat_ThrowsArgumentException(string commandText)
    {
        // Arrange
        var utcOffsetMinutes = 0;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _parser.ParseTimestamp(commandText, utcOffsetMinutes));
        Assert.Contains("Invalid time parameter format", exception.Message);
    }

    [Fact]
    public void ParseTimestamp_MultipleSpacesInParameter_ParsesCorrectly()
    {
        // Arrange
        var commandText = "/work   -15"; // Multiple spaces
        var utcOffsetMinutes = 0;
        var now = DateTime.UtcNow;

        // Act
        var result = _parser.ParseTimestamp(commandText, utcOffsetMinutes);

        // Assert
        var expected = now.AddMinutes(-15);
        Assert.InRange(result, expected.AddSeconds(-1), expected.AddSeconds(1));
    }

    [Fact]
    public void ParseTimestamp_ZeroOffset_ReturnsCurrentTime()
    {
        // Arrange
        var commandText = "/work -0";
        var utcOffsetMinutes = 0;
        var before = DateTime.UtcNow;

        // Act
        var result = _parser.ParseTimestamp(commandText, utcOffsetMinutes);

        // Assert
        var after = DateTime.UtcNow;
        Assert.InRange(result, before.AddSeconds(-1), after.AddSeconds(1));
    }

    #endregion
}
