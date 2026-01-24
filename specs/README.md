# TimeSheet Feature Specifications

## Overview
This folder contains specifications for the TimeSheet MVP application. The MVP includes full state machine with edge cases but defers UI and analytics features.

---

## 📋 MVP Core Features (~1 Hour)

Essential features for basic time tracking with edge case support:

- [WorkDay State Machine](./workday-state-machine.md) - **Full 8-state model with edge cases**
- [User Management](./user-management.md) - Basic user with timezone
- [Time Tracking](./time-tracking.md) - State transition recording
- [Persistence](./persistence.md) - SQLite storage with EF Core

---

## 🚀 Post-MVP Features

Deferred to post-MVP implementation:

- [Telegram Bot](./telegram-bot.md) - Telegram interface
- [Terminal UI](./terminal-ui.md) - Command-line interface
- [Analytics & Reporting](./analytics-reporting.md) - Work pattern insights
- [Notifications](./notifications.md) - Alert system

---

## MVP Philosophy

**What's Included:**
- ✅ Complete state machine (8 states + special states)
- ✅ Edge cases: remote work, emergencies, flexible transitions
- ✅ Core domain logic with comprehensive validation
- ✅ Basic persistence layer

**What's Deferred:**
- ❌ User interfaces (Telegram, TUI)
- ❌ Analytics and reporting
- ❌ Notification system
- ❌ Advanced preferences

The goal is a **solid, well-tested domain model** that handles real-world scenarios (remote work, emergencies) without UI complexity.

---

*Last Updated: January 2026*