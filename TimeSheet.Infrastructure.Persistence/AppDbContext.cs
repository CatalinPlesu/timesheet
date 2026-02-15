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
    }
}
