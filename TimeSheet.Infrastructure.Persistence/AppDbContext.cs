using Microsoft.EntityFrameworkCore;
using TimeSheet.Core.Domain.Entities;

namespace TimeSheet.Infrastructure.Persistence;

/// <summary>
/// The main Entity Framework DbContext for the TimeSheet application.
/// Provides database access and manages entity change tracking.
/// </summary>
public sealed class AppDbContext : DbContext
{
    /// <summary>
    /// Gets or sets the DbSet for TrackingSession entities.
    /// </summary>
    public DbSet<TrackingSession> TrackingSessions => Set<TrackingSession>();

    /// <summary>
    /// Gets or sets the DbSet for User entities.
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// Gets or sets the DbSet for PendingMnemonic entities.
    /// </summary>
    public DbSet<PendingMnemonic> PendingMnemonics => Set<PendingMnemonic>();

    /// <summary>
    /// Gets or sets the DbSet for Holiday entities.
    /// </summary>
    public DbSet<Holiday> Holidays => Set<Holiday>();

    /// <summary>
    /// Gets or sets the DbSet for UserComplianceRule entities.
    /// </summary>
    public DbSet<UserComplianceRule> ComplianceRules => Set<UserComplianceRule>();

    /// <summary>
    /// Gets or sets the DbSet for EmployerAttendanceRecord entities.
    /// </summary>
    public DbSet<EmployerAttendanceRecord> EmployerAttendanceRecords => Set<EmployerAttendanceRecord>();

    /// <summary>
    /// Gets or sets the DbSet for EmployerImportLog entities.
    /// </summary>
    public DbSet<EmployerImportLog> EmployerImportLogs => Set<EmployerImportLog>();

    /// <summary>
    /// Initializes a new instance of the <see cref="AppDbContext"/> class.
    /// </summary>
    /// <param name="dbContextOptions">The DbContext options configured with SQLite provider.</param>
    public AppDbContext(DbContextOptions<AppDbContext> dbContextOptions)
        : base(dbContextOptions)
    {
    }

    /// <summary>
    /// Configures the database schema and entity mappings.
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply entity configurations from assemblies
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // CRITICAL: Configure all DateTime properties to be stored and retrieved as UTC
        // SQLite stores DateTime as TEXT without timezone info.
        // By default, EF Core reads them back as DateTimeKind.Unspecified, which can cause
        // the system to incorrectly interpret them as local time.
        // This converter ensures all DateTime values are explicitly marked as UTC.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(
                        new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
                            v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(), // Ensure UTC when writing to DB
                            v => DateTime.SpecifyKind(v, DateTimeKind.Utc))); // Mark as UTC when reading from DB
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(
                        new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime?, DateTime?>(
                            v => v.HasValue ? (v.Value.Kind == DateTimeKind.Utc ? v.Value : v.Value.ToUniversalTime()) : (DateTime?)null,
                            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : (DateTime?)null));
                }
            }
        }
    }
}
