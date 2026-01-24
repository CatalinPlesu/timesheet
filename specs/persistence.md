# Persistence Specification (MVP)

## 1. Feature Overview

### Purpose
Persistence provides SQLite storage for Users and WorkDays using Entity Framework Core.

---

## 2. Technical Requirements

### Repository Interfaces
```csharp
public interface IUserRepository
{
    Task<User> GetByIdAsync(Guid userId);
    Task<User> AddAsync(User user);
    Task SaveChangesAsync();
}

public interface IWorkDayRepository
{
    Task<WorkDay> GetByUserAndDateAsync(Guid userId, DateOnly date);
    Task<WorkDay> AddAsync(WorkDay workDay);
    Task SaveChangesAsync();
}
```

### DbContext
```csharp
public class TimeSheetDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<WorkDay> WorkDays { get; set; }
    public DbSet<StateTransition> StateTransitions { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite("Data Source=timesheet.db");
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // WorkDay has collection of StateTransitions
        modelBuilder.Entity<WorkDay>()
            .HasMany<StateTransition>()
            .WithOne()
            .HasForeignKey("WorkDayId");
            
        // Unique constraint on UserId+Date
        modelBuilder.Entity<WorkDay>()
            .HasIndex(w => new { w.UserId, w.Date })
            .IsUnique();
    }
}
```

---

## 3. Database Schema

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
- FromState (int)
- ToState (int)
- Timestamp (DateTime)

### Indexes
- Unique index on (UserId, Date) for WorkDays
- Index on WorkDayId for StateTransitions

---

## 4. Implementation Checklist (MVP)

### Persistence Layer (~15 min)
- [ ] Define repository interfaces in Domain layer
- [ ] Create `TimeSheetDbContext` with entity configurations
- [ ] Implement `UserRepository` and `WorkDayRepository`
- [ ] Configure entity relationships and indexes
- [ ] Create initial migration
- [ ] Integration test with in-memory database

---

*Related Features: [User Management](./user-management.md), [Time Tracking](./time-tracking.md)*