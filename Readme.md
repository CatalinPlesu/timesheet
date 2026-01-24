# TimeSheet - Working Hours Tracker

A Clean Architecture application for tracking daily work routines and analyzing work-life balance patterns.

---

## 🎯 Project Overview

**Purpose**: Track daily work transitions (commuting, working, breaks) to gain insights into work patterns and maintain work-life balance.

**Tech Stack**: .NET 8, Clean Architecture, SQLite, Telegram Bot, Terminal UI

---

## 📐 Architecture

```
TimeSheet/
├── Core/
│   ├── Domain/              # Business logic, entities, domain services (no dependencies)
│   └── Application/         # Use cases, DTOs, interfaces (depends on Domain)
├── Infrastructure/
│   └── Persistence/         # EF Core, repositories (depends on Domain)
└── Presentation/
    ├── Telegram/            # Telegram bot (depends on Application)
    └── Tui/                 # Terminal UI (depends on Application)
```

**Dependency Rule**: Dependencies flow inward (Presentation → Application → Domain)

---

## 🧩 Domain Model

### Core Concepts

**Aggregates**:
- `User` - Identity, preferences, external IDs
- `WorkDay` - Daily work session with state transitions

**Value Objects**:
- `UserPreferences` - Work schedule, notification settings
- `ExternalIdentity` - Links to Telegram/TUI

**Entities**:
- `StateTransition` - Records state changes with timestamps

**Enums**:
- `WorkDayState` - Possible states in a workday

---

### WorkDay Aggregate

**States** (WorkDayState):
```
NotStarted → CommutingToWork → AtWork → Working → OnLunch → Working → CommutingHome → AtHome
```

**Valid Transitions**:
- Linear progression through states
- Remote work: `NotStarted → Working` (skip commute)
- No lunch: `Working → CommutingHome`
- Emergency exit: `Any → AtHome`

**Business Rules**:
1. Transitions must be chronological
2. One WorkDay per user per date
3. Transitions must follow valid state flow
4. All timestamps in UTC

**Key Methods**:
- `StartNew(userId, date)` - Factory method
- `RecordTransition(toState, utcNow, timeAdjustment?)` - Main business logic
- `CurrentState` - Derived from latest transition (property)

---

### User Aggregate

**Responsibilities**:
- Manage external identities (Telegram, TUI)
- Store work preferences
- Provide defaults for new WorkDays

**Key Properties**:
- `ExternalIds` - Multiple login methods
- `Preferences` - UserPreferences value object

**Key Methods**:
- `Create(name, timeZone, workDuration)` - Factory
- `LinkExternalIdentity(provider, externalId)`
- `UpdatePreferences(preferences)`

---

### UserPreferences Value Object

**Configuration**:
- `TimeZoneId` - User's timezone
- `ExpectedWorkDuration` - Target hours (e.g., 8h)
- `HasLunchBreak` - Whether lunch is tracked
- `LunchBreakDuration` - Default lunch length
- `NotifyOnMissedTransitions` - Alert settings
- `NotifyWhenWorkHoursComplete` - 8-hour alert
- `WorkDaysSchedule` - Which days are work days

---

## 🎮 User Commands (Telegram/TUI)

Context-aware commands that map to domain transitions:

| Command | Description | Context Behavior |
|---------|-------------|------------------|
| `/commute` | Start travel | To work OR to home (based on current state) |
| `/start` | Start working | Remote work, after arrival, or after lunch |
| `/lunch` | Take lunch break | - |
| `/done` | Finish work | Remote: end day, Office: start commute |
| `/home` | Arrived home | - |
| `/emergency` | Stop tracking | Emergency exit to AtHome |
| `/status` | Current state | Shows progress, next expected action |

**Time Adjustments**: `/start -5m` (5 minutes ago), `/start +3m` (3 minutes future)

---

## 📊 Domain Services

### WorkDayAnalyticsService

**Single-day metrics**:
- `CalculateActualWorkTime(workDay, preferences)` - Exclude lunch if configured
- `CalculateCommuteDuration(workDay, direction)` - To work vs to home
- `CalculateLunchDuration(workDay)`

**Multi-day analytics**:
- `AnalyzeWorkPatterns(workDays, preferences)` - Average work time, trends
- `AnalyzeCommutePatterns(workDays)` - Identify optimal commute times

### WorkDayNotificationService

**Checks**:
- Work hours complete (8h reached)
- Forgot lunch (4+ hours working without break)
- Forgot to clock out

---

## 🗂️ Application Layer

### Commands (Write Operations)

**RecordUserActionCommand**:
- Maps user actions to domain transitions
- Handles context-aware logic (e.g., `/start` behavior)
- Validates against current state

**AdjustLastTransitionCommand**:
- Corrects timing of last transition
- Validates chronological order

**StopTrackingCommand**:
- Emergency exit from any state

### Queries (Read Operations)

**GetCurrentStatusQuery**:
- Current state
- Time in current state
- Work time so far
- Next expected action

**GetTodayTimelineQuery**:
- All transitions for today
- Calculated durations

**GetWorkStatsQuery**:
- Date range statistics
- Daily averages
- Commute trends

---

## 🏗️ Implementation Notes

### Domain Layer
- **No infrastructure dependencies** (no EF, no external libs)
- Entities validate themselves through methods
- Factory methods (`Create`, `StartNew`) for construction
- Rich domain model (behavior, not anemic)

### Repositories
- **Interfaces in Domain** (e.g., `IWorkDayRepository`, `IUserRepository`)
- **Implementations in Infrastructure.Persistence**
- Return domain entities, never expose EF details
- Use async/await for all operations

### Application Services
- Orchestrate domain objects
- Handle transaction boundaries
- Map between domain and DTOs
- No business logic (delegate to domain)

---

## ✅ Implementation Checklist

### Phase 1: Domain Foundation
- [ ] `WorkDayState` enum
- [ ] `StateTransition` entity with `Create()` factory
- [ ] `WorkDay` aggregate with transition validation
- [ ] `User` aggregate with external identities
- [ ] `UserPreferences` value object
- [ ] Unit tests for all domain logic

### Phase 2: Persistence
- [ ] Repository interfaces in Domain
- [ ] EF Core DbContext
- [ ] Repository implementations
- [ ] Migrations
- [ ] Integration tests

### Phase 3: Application Layer
- [ ] `RecordUserActionCommand` + handler
- [ ] `GetCurrentStatusQuery` + handler
- [ ] Domain service implementations
- [ ] Application tests

### Phase 4: Telegram Bot
- [ ] Command registration
- [ ] Message formatting
- [ ] User session management
- [ ] Bot tests

---

## 🧪 Testing Strategy

**Domain Tests** (Unit):
- State transition validation
- Business rule enforcement
- Edge cases (emergency exits, remote work)

**Application Tests** (Integration):
- Command handlers with in-memory repos
- Query handlers
- Domain service logic

**Presentation Tests** (E2E):
- Bot command flows
- User scenarios

---

## 🚀 Getting Started

1. **Start with Domain**: Build entities with `throw new NotImplementedException()` stubs
2. **Write Tests First**: Define expected behavior before implementation
3. **One Class at a Time**: Don't build everything at once
4. **Validate Early**: Test business rules in domain, not in application layer

---

## 📝 Key Insights

### DDD Principles Applied
- **Ubiquitous Language**: `WorkDay`, `StateTransition`, `UserAction` (not "record", "log")
- **Aggregates**: `WorkDay` and `User` enforce consistency boundaries
- **Value Objects**: `UserPreferences`, `ExternalIdentity` (immutable, no identity)
- **Domain Services**: Cross-aggregate operations (analytics)
- **Factory Methods**: Enforce valid construction

### Clean Architecture Benefits
- **Testable**: Domain has zero dependencies
- **Flexible**: Swap Telegram for Discord without changing domain
- **Maintainable**: Business logic isolated from infrastructure

### Validation Strategy
- **Domain validates state transitions**: `WorkDay.RecordTransition()`
- **Application validates user actions**: Can `/start` be performed now?
- **Presentation validates input format**: Is `-5m` a valid time adjustment?

---

## 🤔 Edge Cases Handled

- **Remote work**: Skip commute states
- **No lunch break**: Direct Working → CommutingHome
- **Emergency exit**: Any state → AtHome
- **Time adjustments**: Record actions in the past
- **Chronological validation**: Prevent out-of-order transitions
- **Date boundaries**: Transitions must match WorkDay date

---

## 📚 Resources

- **Clean Architecture**: Uncle Bob's principles
- **DDD**: Eric Evans - Domain-Driven Design
- **C# Patterns**: Switch expressions, LINQ, factory methods
- **Testing**: xUnit, FluentAssertions

---

**Version**: 1.0  
**Last Updated**: January 2026
