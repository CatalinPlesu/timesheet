# Time Tracking Specification (MVP)

## 1. Feature Overview

### Purpose
Time Tracking manages recording of work state transitions with full support for edge cases (remote work, emergencies).

### Key Concepts
- **State Transition Recording**: Capture all 8 regular states plus edge cases
- **Validation**: Ensure transitions follow business rules
- **Edge Case Support**: Handle remote work and emergencies

---

## 2. Technical Requirements

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

### Business Rules
1. **Validate Transition**: Check if valid from current state (including edge cases)
2. **Chronological Order**: Timestamp must be after last transition
3. **Create If Needed**: Auto-create WorkDay if it doesn't exist
4. **Edge Case Support**: Handle remote work skips and emergency exits

---

## 3. Implementation Checklist (MVP)

### Application Layer (~10 min)
- [ ] Implement `RecordTransitionCommand` handler
  - Find or create WorkDay
  - Call `WorkDay.RecordTransition()`
  - Handle edge cases
  - Save to repository
- [ ] Implement `GetCurrentStatusQuery` handler
  - Find WorkDay
  - Return current state and transitions
- [ ] Integration test with in-memory repository

---

*Related Features: [WorkDay State Machine](./workday-state-machine.md), [Persistence](./persistence.md)*