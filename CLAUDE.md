# TimeSheet

A private Telegram bot for personal work-hour tracking. Not employer surveillance —
the goal is to help the user ensure work doesn't take more time than it should.

**Stack**: .NET 10 / C# 14, SQLite (persistence), Telegram (presentation)

## Project Overview

### Core Commands

| Command | Alias | Purpose |
|---------|-------|---------|
| `/commute` | `/c` | Track commute (to work or to home) |
| `/work` | `/w` | Track work time |
| `/lunch` | `/l` | Track lunch break |

**Optional parameters** (apply to all commands):
- `-m` / `+m` — action started m minutes ago / will start m minutes from now
- `[hh:mm]` — exact start time in 24h format

**Toggle behavior**: Commands are exclusive and act as toggles.
- Starting a new state stops the previous one: `/commute` → `/work` stops commute, starts work
- Repeating the same command stops it: `/commute` → `/commute` stops commute
- Valid sequences like `/commute /work /lunch /work /commute` are natural

**Commute directionality**: The system distinguishes commute-to-work vs commute-to-home
based on context (first commute of the day = to work, commute after work = to home).
This enables analysis of which direction is faster and optimization suggestions.

**Work sessions**: Multiple work sessions per day are summed (split by lunch, etc.).

### Editing & Corrections

- `/edit` — edit the most recent entry (no args), or N entries back (`/edit 1`, `/edit 2`, `/edit 3`)
- Inline keyboard with adjustment buttons: `-30m` `-5m` `-1m` `+1m` `+5m` `+30m` (clickable multiple times)
- `/delete` — delete an entry, with confirmation prompt

### Authentication

Private bot — not multi-tenant. Registration flow:
1. If no users exist, first registrant becomes admin
2. Console logs a `/register [24-word BIP39 mnemonic]` command
3. Admin copies this into the Telegram bot to register
4. Admin can pre-generate multiple mnemonics for other users
5. Mnemonics are single-use (removed after registration)
6. Bot ignores all requests from non-registered users (except `/about`)

### User Settings

- **UTC offset** — entered on registration (Telegram doesn't provide timezone). Editable later.
- **Auto-shutdown** — automatically close a forgotten state (e.g., left "working" on when going home).
  Configurable per state: absolute time threshold or percentage of normal (e.g., 130%).

### Notifications (later epic)

- Take lunch reminder (user sets hour)
- N hours of work completed
- Forgot to shut down a state (based on averages)

### Reports (later epic)

- Average daily: worked hours, commute time, lunch duration
- Commute-to-work pattern: when to leave for shortest commute (varies by day of week)
- Commute-to-home pattern: same analysis
- Weekly, monthly, yearly aggregates for all metrics

### Epic Roadmap (rough)

1. **Project setup** — solution scaffold, layers, infrastructure
2. **Base tracking** — core commands, toggle logic, state machine, persistence
3. **Authentication & registration** — mnemonic flow, admin, user management
4. **Editing & corrections** — `/edit`, `/delete`, inline keyboards
5. **Settings** — UTC offset, auto-shutdown configuration
6. **Notifications** — reminders, alerts
7. **Reports** — analytics, patterns, suggestions

## Git Policy

- **main** is the trunk branch — never commit directly to it
- All work happens on feature/task branches
- Merge into main with **squash** (`git merge --squash`)
- Pushing to remote is authorized — do not ask for confirmation (beads workflow requires it)

## Agent Instructions

This project uses **bd** (beads) for issue tracking. Run `bd onboard` to get started.

### Quick Reference

```bash
bd ready              # Find available work
bd show <id>          # View issue details
bd update <id> --status in_progress  # Claim work
bd close <id>         # Complete work
bd sync               # Sync with git
```

### Landing the Plane (Session Completion)

**When ending a work session**, you MUST complete ALL steps below. Work is NOT complete until `git push` succeeds.

**MANDATORY WORKFLOW:**

1. **File issues for remaining work** - Create issues for anything that needs follow-up
2. **Run quality gates** (if code changed) - Tests, linters, builds
3. **Update issue status** - Close finished work, update in-progress items
4. **PUSH TO REMOTE** - This is MANDATORY:
   ```bash
   git pull --rebase
   bd sync
   git push
   git status  # MUST show "up to date with origin"
   ```
5. **Clean up** - Clear stashes, prune remote branches
6. **Verify** - All changes committed AND pushed
7. **Hand off** - Provide context for next session

**CRITICAL RULES:**
- Work is NOT complete until `git push` succeeds
- NEVER stop before pushing - that leaves work stranded locally
- NEVER say "ready to push when you are" - YOU must push
- If push fails, resolve and retry until it succeeds
