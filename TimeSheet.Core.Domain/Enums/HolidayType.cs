namespace TimeSheet.Core.Domain.Enums;

/// <summary>
/// Represents the type of non-working day.
/// Used to categorize different types of time off.
/// </summary>
public enum HolidayType
{
    /// <summary>
    /// Public or national holiday.
    /// </summary>
    Holiday = 0,

    /// <summary>
    /// Personal vacation day or paid time off.
    /// </summary>
    Vacation = 1,

    /// <summary>
    /// Sick leave or medical absence.
    /// </summary>
    Sick = 2
}
