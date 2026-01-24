# Time Tracking Specification (MVP)

## Overview
Time Tracking manages recording work state transitions with timestamps.

---

## Core Operations

### Record Transition
- Record a state change for a workday
- Validate transition is valid for current state
- Ensure chronological order

### Get Current Status
- Retrieve current state for a user's workday
- Show today's transitions

---

## Domain Model

### Commands
```csharp
public class RecordTransitionCommand
{
    public Guid UserId { get; set; }
    public DateOnly Date { get; set; }
    public WorkDayState ToState { get; set; }
    public DateTime Timestamp { get; set; }
}
```

### Queries
```csharp
public class GetCurrentStatusQuery
{
    public Guid UserId { get; set; }
    public DateOnly Date { get; set; }
}

public class CurrentStatusResult
{
    public WorkDayState CurrentState { get; set; }
    public List<StateTransition> Transitions { get; set; }
}
```

---

## Business Rules

1. **Validate Transition**: Check if transition is valid from current state
2. **Chronological Order**: Timestamp must be after last transition
3. **Create If Needed**: If no workday exists for date, create one
4. **Same Date**: All transitions must be on the same date as the workday

---

## Implementation Checklist

- [ ] Implement `RecordTransitionCommand` handler
  - Find or create WorkDay for user and date
  - Call `WorkDay.RecordTransition()`
  - Save to repository
- [ ] Implement `GetCurrentStatusQuery` handler
  - Find WorkDay for user and date
  - Return current state and transitions
- [ ] Write integration tests with in-memory repository

---

## Testing

### Unit Tests
- Recording valid transition should succeed
- Recording invalid transition should fail
- Out-of-order transitions should be rejected
- Creating new workday should default to NotStarted

### Integration Tests
- Full workflow: Create workday → Record transitions → Query status
- Multiple transitions in sequence
- Query non-existent workday returns NotStarted

---

*Related: [WorkDay State Machine](./workday-state-machine.md), [Persistence](./persistence.md)*
