# TimeSheet.Infrastructure.Persistence

This layer provides Entity Framework Core infrastructure for SQLite persistence.

## Components

### AppDbContext
The main EF Core DbContext for the application. Features:
- Automatic `UpdatedAt` timestamp management for `MutableEntity` instances
- Configuration via IOptions pattern
- Prepared for entity configuration assembly scanning

### DatabaseOptions
Configuration class bound from `appsettings.json` "Database" section:
- `ConnectionString`: SQLite connection string (e.g., "Data Source=timesheet.db")
- `EnableSensitiveDataLogging`: Enable EF Core sensitive data logging (dev only)
- `EnableDetailedErrors`: Enable EF Core detailed errors (dev only)

## Configuration

### appsettings.json
```json
{
  "Database": {
    "ConnectionString": "Data Source=timesheet.db",
    "EnableSensitiveDataLogging": false,
    "EnableDetailedErrors": false
  }
}
```

### Dependency Injection Setup (Future)
```csharp
// In DI extension (TimeSheet-3dj.6):
services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));

services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    var dbOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;

    options.UseSqlite(dbOptions.ConnectionString);

    if (dbOptions.EnableSensitiveDataLogging)
        options.EnableSensitiveDataLogging();

    if (dbOptions.EnableDetailedErrors)
        options.EnableDetailedErrors();
});
```

## NuGet Packages
- `Microsoft.EntityFrameworkCore.Sqlite` (10.0.2)
- `Microsoft.EntityFrameworkCore.Design` (10.0.2)

## Automatic Timestamp Management

The `AppDbContext.SaveChangesAsync` override automatically updates `UpdatedAt` timestamps for all modified `MutableEntity` instances before persisting changes. This ensures timestamp consistency without manual intervention.

## Future Work
- Entity configurations (TimeSheet-3dj.4+)
- Repository implementations (TimeSheet-3dj.4)
- Unit of Work pattern (TimeSheet-3dj.4)
- EF Core migrations (later epics)
