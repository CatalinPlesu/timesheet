# User Management Specification (MVP)

## 1. Feature Overview

### Purpose
User Management handles basic user profiles with timezone information for timestamp display.

### Key Concepts
- **User**: Basic aggregate with identity and timezone
- **Timezone**: UTC offset for displaying local times

### User Stories
- **As a developer**, I want simple user management without complex preferences
- **As a user**, I want my times displayed in my local timezone

---

## 2. Technical Requirements

### Data Models
- **User Entity**: Name and timezone offset only

### Business Rules
1. **Required Fields**: Name and UtcOffsetHours are required
2. **Valid Timezone**: UtcOffsetHours must be between -12 and +14
3. **Non-Empty Name**: Name cannot be empty

### API Requirements
- User creation with validation
- User lookup by ID

---

## 3. Implementation Details

### Domain Model
```csharp
public class User
{
    public Guid Id { get; }
    public string Name { get; }
    public int UtcOffsetHours { get; }
    
    public static User Create(string name, int utcOffsetHours);
}
```

### Repository Interface
```csharp
public interface IUserRepository
{
    Task<User> GetByIdAsync(Guid userId);
    Task<User> AddAsync(User user);
    Task SaveChangesAsync();
}
```

---

## 4. Implementation Checklist (MVP)

### User Entity (~5 min)
- [ ] Implement User entity with validation
- [ ] Factory method `Create()` with name/timezone validation
- [ ] Unit tests for creation and validation

---

*Related Features: [WorkDay State Machine](./workday-state-machine.md), [Persistence](./persistence.md)*