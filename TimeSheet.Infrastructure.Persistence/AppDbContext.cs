using Microsoft.EntityFrameworkCore;
using TimeSheet.Core.Domain.Entities;
using TimeSheet.Core.Domain.SharedKernel;

namespace TimeSheet.Infrastructure.Persistence;

/// <summary>
/// The main Entity Framework DbContext for the TimeSheet application.
/// Provides database access and manages entity change tracking.
/// </summary>
public sealed class AppDbContext : DbContext
{
    /// <summary>
    /// Gets or sets the DbSet for TimeEntry entities.
    /// </summary>
    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();

    /// <summary>
    /// Gets or sets the DbSet for TrackingSession entities.
    /// </summary>
    public DbSet<TrackingSession> TrackingSessions => Set<TrackingSession>();

    /// <summary>
    /// Initializes a new instance of the <see cref="AppDbContext"/> class.
    /// </summary>
    /// <param name="dbContextOptions">The DbContext options configured with SQLite provider.</param>
    public AppDbContext(DbContextOptions<AppDbContext> dbContextOptions)
        : base(dbContextOptions)
    {
    }

    /// <summary>
    /// Saves all changes made in this context to the database.
    /// Automatically updates UpdatedAt timestamps for modified MutableEntity instances.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The number of state entries written to the database.</returns>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Auto-update timestamps for modified MutableEntity instances
        UpdateTimestamps();

        return await base.SaveChangesAsync(cancellationToken);
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

    /// <summary>
    /// Updates UpdatedAt timestamps for all modified MutableEntity instances in the change tracker.
    /// Calls MarkAsModified() directly on each modified entity to set the UpdatedAt timestamp.
    /// </summary>
    private void UpdateTimestamps()
    {
        var modifiedEntities = ChangeTracker
            .Entries<MutableEntity>()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in modifiedEntities)
        {
            entry.Entity.MarkAsModified();
        }
    }
}
