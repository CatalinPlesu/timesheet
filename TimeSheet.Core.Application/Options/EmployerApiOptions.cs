namespace TimeSheet.Core.Application.Options;

/// <summary>
/// Configuration options for the employer's attendance API.
/// Bound from the "EmployerApi" section in appsettings.json.
/// </summary>
/// <remarks>
/// <c>BaseUrl</c> must be set to the employer's API base URL before the import feature can be used.
/// Example: "https://timily.example.com"
/// </remarks>
public record EmployerApiOptions : IOptionsWithSectionName
{
    /// <summary>
    /// Configuration section name for binding.
    /// </summary>
    public static string SectionName => "EmployerApi";

    /// <summary>
    /// Gets the base URL of the employer's attendance API.
    /// Leave empty until configured — the import feature will be disabled when empty.
    /// </summary>
    public string BaseUrl { get; init; } = string.Empty;

    /// <summary>
    /// Gets the relative endpoint path for the employee attendance query.
    /// </summary>
    public string AttendanceEndpoint { get; init; } = "/api/TimeOff/getEmployeeAttendance";
}
