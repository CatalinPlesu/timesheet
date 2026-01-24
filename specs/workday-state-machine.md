# WorkDay State Machine Specification (MVP)

## Overview
The WorkDay State Machine manages the lifecycle of a single workday with simple state transitions.

---

## States

```
NotStarted → Working → OnLunch → Working → Finished
```

- **NotStarted**: Day hasn't begun
- **Working**: Actively working
- **OnLunch**: Taking a lunch break
- **Finished**: Work completed for the day

---

## Valid Transitions

- `NotStarted → Working` - Start work
- `Working → OnLunch` - Take lunch break
- `OnLunch → Working` - Return from lunch
- `Working → Finished` - End workday

---

## Business Rules

1. **Chronological Order**: Transitions must be in time order
2. **One WorkDay Per User Per Date**: Each user can have only one workday per date
3. **Valid State Flow**: Can only transition to valid next states
4. **UTC Storage**: All timestamps stored as UTC

---

## Domain Model

### WorkDay Aggregate
```csharp
public class WorkDay
{
    public Guid Id { get; }
    public Guid UserId { get; }
    public DateOnly Date { get; }
    public WorkDayState CurrentState { get; }
    
    public static WorkDay StartNew(Guid userId, DateOnly date);
    public void RecordTransition(WorkDayState toState, DateTime timestamp);
}
```

### StateTransition Entity
```csharp
public class StateTransition
{
    public Guid Id { get; }
    public WorkDayState FromState { get; }
    public WorkDayState ToState { get; }
    public DateTime Timestamp { get; }
}
```

### WorkDayState Enum
```csharp
public enum WorkDayState
{
    NotStarted,
    Working,
    OnLunch,
    Finished
}
```

---

## Implementation Checklist

- [ ] Create `WorkDayState` enum with 4 states
- [ ] Implement `StateTransition` entity
- [ ] Implement `WorkDay` aggregate with:
  - Factory method `StartNew()`
  - `RecordTransition()` with validation
  - `CurrentState` property
- [ ] Add transition validation logic
- [ ] Write unit tests for valid/invalid transitions

---

## Testing

### Unit Tests
- Valid transitions should succeed
- Invalid transitions should throw exception
- Non-chronological transitions should be rejected
- CurrentState should reflect latest transition

### Example Test Cases
```csharp
// Valid flow
WorkDay.StartNew() -> NotStarted
RecordTransition(Working) -> Success
RecordTransition(OnLunch) -> Success
RecordTransition(Working) -> Success
RecordTransition(Finished) -> Success

// Invalid flow
RecordTransition(Finished) from NotStarted -> Exception
RecordTransition(OnLunch) from NotStarted -> Exception
```

---

*Related: [Time Tracking](./time-tracking.md), [User Management](./user-management.md)*
