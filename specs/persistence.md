# Persistence Specification (MVP)

## Overview
Persistence layer provides SQLite storage for Users and WorkDays using Entity Framework Core.

---

## Technology

- **Database**: SQLite (embedded, no server needed)
- **ORM**: Entity Framework Core
- **Migrations**: Code-first migrations

---

## Repository Implementations

### IUserRepository
- `GetByIdAsync(userId)` - Find user by ID
- `AddAsync(user)` - Add new user
- `SaveChangesAsync()` - Commit changes

### IWorkDayRepository
- `GetByUserAndDateAsync(userId, date)` - Find workday
- `AddAsync(workDay)` - Add new workday
- `SaveChangesAsync()` - Commit changes

---

## DbContext

```csharp
public class TimeSheetDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<WorkDay> WorkDays { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite("Data Source=timesheet.db");
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure entity mappings
        modelBuilder.Entity<WorkDay>()
            .HasIndex(w => new { w.UserId, w.Date })
            .IsUnique();
    }
}
```

---

## Database Schema

### Users Table
- Id (Guid, PK)
- Name (string)
- UtcOffsetHours (int)

### WorkDays Table
- Id (Guid, PK)
- UserId (Guid, FK)
- Date (Date)

### StateTransitions Table
- Id (Guid, PK)
- WorkDayId (Guid, FK)
- FromState (int/enum)
- ToState (int/enum)
- Timestamp (DateTime)

### Indexes
- Unique index on (UserId, Date) for WorkDays
- Index on WorkDayId for StateTransitions

---

## Implementation Checklist

- [ ] Create `TimeSheetDbContext` with DbSets
- [ ] Implement repository interfaces in Infrastructure layer
- [ ] Add entity configurations for EF Core
- [ ] Create initial migration
- [ ] Write integration tests with in-memory database

---

## Testing

### Integration Tests
- Save and retrieve User
- Save and retrieve WorkDay
- Unique constraint on UserId+Date enforced
- Transitions are persisted correctly

---

*Related: [User Management](./user-management.md), [Time Tracking](./time-tracking.md)*
