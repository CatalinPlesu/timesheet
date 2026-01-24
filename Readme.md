# TimeSheet - Working Hours Tracker

A Clean Architecture application for tracking daily work routines with support for remote work and edge cases.

---

## 🎯 Project Overview

**Purpose**: Track daily work transitions (commuting, working, breaks) with flexible state management for office and remote work.

**Tech Stack**: .NET 8, Clean Architecture, SQLite, Telegram Bot, Terminal UI

**MVP Focus**: Core domain with full state machine (8 states + edge cases), basic persistence, Telegram/TUI interfaces

---

## 📐 Architecture

```
TimeSheet/
├── Core/
│   ├── Domain/              # Business logic, entities (no dependencies)
│   └── Application/         # Use cases, interfaces (depends on Domain)
├── Infrastructure/
│   └── Persistence/         # EF Core, repositories (depends on Domain)
└── Presentation/
    ├── Telegram/            # Telegram bot (depends on Application) - MVP Ready
    └── Tui/                 # Terminal UI (depends on Application) - MVP Ready
```

**Dependency Rule**: Dependencies flow inward (Presentation → Application → Domain)

---

## 🧩 Domain Model

### Core Concepts

**Aggregates**:
- `User` - Identity and timezone
- `WorkDay` - Daily work session with state transitions

**Entities**:
- `StateTransition` - Records state changes with timestamps

**Enums**:
- `WorkDayState` - All workday states (8 regular + 3 special)

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
4. All timestamps stored as UTC DateTime
5. TimeOnly used for transition times within the day

**Key Methods**:
- `StartNew(userId, date)` - Factory method
- `RecordTransition(toState, time)` - Main business logic with TimeOnly
- `CurrentState` - Derived from latest transition (property)

---

### User Aggregate

**Responsibilities**:
- Manage identity and timezone

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
- Validates against current state and business rules
- Handles edge cases (remote work, emergencies)

### Queries (Read Operations)

**GetCurrentStatusQuery**:
- Current state and today's transitions
- Next expected actions

---

## ✅ Implementation Checklist (MVP - ~2 Hours)

### Phase 1: Domain Foundation (35 min)
- [ ] `WorkDayState` enum (11 states: 8 regular + 3 special)
- [ ] `StateTransition` entity with timestamp
- [ ] `WorkDay` aggregate with transition validation
  - All 8-state transitions
  - Remote work support (skip commute)
  - Emergency exit (any → AtHome)
  - No lunch option
- [ ] `User` entity with name and timezone
- [ ] Unit tests for state transitions and edge cases

### Phase 2: Persistence (15 min)
- [ ] Repository interfaces in Domain (IUserRepository, IWorkDayRepository)
- [ ] EF Core DbContext
- [ ] Repository implementations
- [ ] Initial migration

### Phase 3: Application Layer (10 min)
- [ ] `RecordTransitionCommand` + handler
- [ ] `GetCurrentStatusQuery` + handler
- [ ] Basic integration test

### Phase 4: Telegram Bot (30 min)
- [ ] Bot command handlers (/start, /commute, /lunch, /done, /home, /emergency, /status)
- [ ] User authentication/registration via Telegram
- [ ] Message formatting and responses
- [ ] Context-aware command behavior

### Phase 5: Terminal UI (Optional - 30 min)
- [ ] CLI command parsing
- [ ] Interactive mode
- [ ] Status display

---

## 🧪 Testing Strategy

**Domain Tests** (Unit):
- All state transition paths (8 states)
- Edge cases: remote work, emergency exit, no lunch
- Business rule enforcement
- Chronological validation

**Application Tests** (Integration):
- Command handlers with in-memory repos
- Query handlers
- End-to-end state flows

---

## 🚀 Getting Started

1. **Domain First**: Implement WorkDay with all 8 states + edge cases
2. **Test Coverage**: Focus on transition validation and edge cases
3. **Telegram/TUI**: Simple interfaces for time tracking
4. **SQLite Storage**: Basic persistence

---

## 📝 MVP Scope

### ✅ Included (Essential for MVP):
- Full 8-state machine (NotStarted → CommutingToWork → AtWork → Working → OnLunch → Working → CommutingHome → AtHome)
- Edge cases: Remote work, Emergency exit, No lunch
- User entity (name + timezone)
- Basic persistence (SQLite)
- Telegram bot interface
- Terminal UI (TUI) interface
- Command/query pattern

### ❌ Post-MVP (Future):
- Analytics and reporting
- Notifications and reminders
- Advanced preferences (schedules, holidays)
- Deployment configurations

---

**Version**: 1.0 MVP with Edge Cases  
**Last Updated**: January 2026
