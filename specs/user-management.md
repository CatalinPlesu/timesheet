# User Management Specification (MVP)

## Overview
User Management handles basic user profiles with timezone information.

---

## Core Operations

### Create User
- Create a new user with name and timezone
- Return user ID for future operations

### Get User
- Retrieve user by ID
- Return user profile information

---

## Domain Model

### User Entity
```csharp
public class User
{
    public Guid Id { get; }
    public string Name { get; }
    public int UtcOffsetHours { get; }
    
    public static User Create(string name, int utcOffsetHours);
}
```

---

## Business Rules

1. **Required Fields**: Name and UtcOffsetHours are required
2. **Valid Timezone**: UtcOffsetHours must be between -12 and +14
3. **Non-Empty Name**: Name cannot be empty or whitespace

---

## Repository Interface

```csharp
public interface IUserRepository
{
    Task<User> GetByIdAsync(Guid userId);
    Task<User> AddAsync(User user);
    Task SaveChangesAsync();
}
```

---

## Implementation Checklist

- [ ] Implement `User` entity with:
  - Factory method `Create()`
  - Validation for name and timezone
- [ ] Define `IUserRepository` interface in Domain
- [ ] Write unit tests for User creation and validation

---

## Testing

### Unit Tests
- Creating user with valid data should succeed
- Creating user with empty name should fail
- Creating user with invalid timezone should fail
- User properties should be immutable after creation

---

*Related: [WorkDay State Machine](./workday-state-machine.md), [Persistence](./persistence.md)*
