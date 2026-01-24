# TimeSheet Feature Specifications

## Overview
This folder contains specifications for the TimeSheet MVP application. The MVP includes full state machine with edge cases, plus Telegram and Terminal UI interfaces.

---

## 📋 MVP Core Features (~2 Hours)

Essential features for complete time tracking system:

### Core Domain (~1 Hour)
- [WorkDay State Machine](./workday-state-machine.md) - **Full 8-state model with edge cases**
- [User Management](./user-management.md) - Basic user with timezone
- [Time Tracking](./time-tracking.md) - State transition recording
- [Persistence](./persistence.md) - SQLite storage with EF Core

### User Interfaces (~1 Hour)
- [Telegram Bot](./telegram-bot.md) - Telegram interface for remote tracking
- [Terminal UI](./terminal-ui.md) - Command-line interface for local tracking

---

## 🚀 Post-MVP Features

Deferred to post-MVP implementation:

- [Analytics & Reporting](./analytics-reporting.md) - Work pattern insights
- [Notifications](./notifications.md) - Alert system

---

## MVP Philosophy

**What's Included:**
- ✅ Complete state machine (8 states + special states)
- ✅ Edge cases: remote work, emergencies, flexible transitions
- ✅ Core domain logic with comprehensive validation
- ✅ Basic persistence layer
- ✅ Telegram bot interface
- ✅ Terminal UI interface

**What's Deferred:**
- ❌ Analytics and reporting
- ❌ Notification system
- ❌ Advanced preferences

The goal is a **complete working system** with solid domain model and usable interfaces for real-world time tracking.

---

*Last Updated: January 2026*