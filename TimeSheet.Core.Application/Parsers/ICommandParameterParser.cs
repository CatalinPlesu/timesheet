namespace TimeSheet.Core.Application.Parsers;

/// <summary>
/// Parses optional time parameters from tracking commands.
/// </summary>
public interface ICommandParameterParser
{
    /// <summary>
    /// Parses a timestamp from command text, applying user's UTC offset.
    /// </summary>
    /// <param name="commandText">The full command text (e.g., "/work -15" or "/commute [14:30]")</param>
    /// <param name="userUtcOffsetMinutes">User's UTC offset in minutes (e.g., +60 for UTC+1, -300 for UTC-5)</param>
    /// <returns>UTC DateTime for persistence</returns>
    /// <exception cref="ArgumentException">Thrown when parameter format is invalid</exception>
    DateTime ParseTimestamp(string commandText, int userUtcOffsetMinutes);
}
