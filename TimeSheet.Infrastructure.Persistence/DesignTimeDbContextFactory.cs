using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TimeSheet.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for creating AppDbContext instances.
/// Used by EF Core tools (migrations, scaffolding) at design time.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        // Use a temporary in-memory database for design-time operations
        // The actual connection string is configured at runtime via appsettings.json
        optionsBuilder.UseSqlite("Data Source=:memory:");

        return new AppDbContext(optionsBuilder.Options);
    }
}
