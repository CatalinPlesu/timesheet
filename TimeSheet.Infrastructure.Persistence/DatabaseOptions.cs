using TimeSheet.Core.Application.Options;

namespace TimeSheet.Infrastructure.Persistence;

/// <summary>
/// Configuration options for database connection and behavior.
/// Bound from the "Database" section in appsettings.json.
/// </summary>
public sealed class DatabaseOptions : IOptionsWithSectionName
{
    /// <summary>
    /// Configuration section name for binding.
    /// </summary>
    public static string SectionName => "Database";

    /// <summary>
    /// Gets or sets the SQLite database connection string.
    /// </summary>
    /// <remarks>
    /// Example: "Data Source=timesheet.db"
    /// For in-memory testing: "Data Source=:memory:"
    /// </remarks>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to enable sensitive data logging for EF Core.
    /// Should only be enabled in development environments.
    /// </summary>
    public bool EnableSensitiveDataLogging { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to enable detailed error messages for EF Core.
    /// Should only be enabled in development environments.
    /// </summary>
    public bool EnableDetailedErrors { get; set; } = false;
}
