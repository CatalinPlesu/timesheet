# TimeSheet - Working Hours Tracker

A Clean Architecture application for tracking daily work routines.

---

## 🎯 Project Overview

**Purpose**: Track daily work transitions (working, breaks) with simple state management.

**Tech Stack**: .NET 8, Clean Architecture, SQLite

**Scope**: Basic MVP - core time tracking only, no UI/notifications for now

---

## 📐 Architecture

```
TimeSheet/
├── Core/
│   ├── Domain/              # Business logic, entities (no dependencies)
│   └── Application/         # Use cases, interfaces (depends on Domain)
└── Infrastructure/
    └── Persistence/         # EF Core, repositories (depends on Domain)
```

**Dependency Rule**: Dependencies flow inward (Presentation → Application → Domain)

---

## 🧩 Domain Model

### Core Concepts

**Aggregates**:
- `User` - Basic identity with timezone
- `WorkDay` - Daily work session with state transitions

**Entities**:
- `StateTransition` - Records state changes with timestamps

**Enums**:
- `WorkDayState` - Possible states (NotStarted, Working, OnLunch, Finished)

---

### WorkDay Aggregate

**States** (WorkDayState):
```
NotStarted → Working → OnLunch → Working → Finished
```

**Valid Transitions**:
- Simple progression: NotStarted → Working
- Optional lunch: Working → OnLunch → Working
- End day: Working → Finished

**Business Rules**:
1. Transitions must be chronological
2. One WorkDay per user per date
3. Transitions must follow valid state flow
4. All timestamps stored as UTC DateTime

**Key Methods**:
- `StartNew(userId, date)` - Factory method
- `RecordTransition(toState, timestamp)` - Main business logic
- `CurrentState` - Derived from latest transition

---

### User Aggregate

**Responsibilities**:
- Manage identity and basic profile
- Store timezone preference for timestamp display

**Key Properties**:
- `Name` - User name
- `UtcOffsetHours` - Timezone offset

**Key Methods**:
- `Create(name, utcOffsetHours)` - Factory

---

## 🗂️ Application Layer

### Commands (Write Operations)

**RecordTransitionCommand**:
- Records state transitions for a workday
- Validates chronological order

### Queries (Read Operations)

**GetCurrentStatusQuery**:
- Current state
- Today's transitions

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

## ✅ Implementation Checklist (MVP)

### Phase 1: Domain Foundation (30 min)
- [ ] `WorkDayState` enum (NotStarted, Working, OnLunch, Finished)
- [ ] `StateTransition` entity with timestamp
- [ ] `WorkDay` aggregate with basic transition validation
- [ ] `User` entity with name and timezone
- [ ] Basic unit tests

### Phase 2: Persistence (20 min)
- [ ] Repository interfaces in Domain
- [ ] EF Core DbContext
- [ ] Repository implementations
- [ ] Initial migration

### Phase 3: Application Layer (10 min)
- [ ] `RecordTransitionCommand` + handler
- [ ] `GetCurrentStatusQuery` + handler
- [ ] Basic integration test

---

## 🧪 Testing Strategy

**Domain Tests** (Unit):
- State transition validation
- Chronological order enforcement

**Application Tests** (Integration):
- Command handlers with in-memory repos
- Query handlers

---

## 🚀 Getting Started

1. **Start with Domain**: Build entities with core logic
2. **Write Tests First**: Define expected behavior
3. **One Class at a Time**: Keep it simple
4. **SQLite for Storage**: No external dependencies

---

## 📝 MVP Scope

### Included:
- ✅ Basic workday state tracking (NotStarted → Working → OnLunch → Finished)
- ✅ User management with timezone
- ✅ SQLite persistence
- ✅ Simple command/query pattern

### Future Enhancements (Not MVP):
- ❌ Telegram bot / Terminal UI
- ❌ Analytics and reporting
- ❌ Notifications
- ❌ Commute tracking
- ❌ Multiple external identities
- ❌ Advanced preferences (work schedules, holidays, etc.)
- ❌ Deployment and production features

---

## 📝 MVP Scope

### Included:
- ✅ Basic workday state tracking (NotStarted → Working → OnLunch → Finished)
- ✅ User management with timezone
- ✅ SQLite persistence
- ✅ Simple command/query pattern

### Future Enhancements (Not MVP):
- ❌ Telegram bot / Terminal UI
- ❌ Analytics and reporting
- ❌ Notifications
- ❌ Commute tracking
- ❌ Multiple external identities
- ❌ Advanced preferences (work schedules, holidays, etc.)
- ❌ Deployment and production features

---

**Version**: 1.0 MVP  
**Last Updated**: January 2026
