# TimeSheet App - Quick Start Guide

## 🚀 Getting Started

### Prerequisites
- .NET 10.0 SDK installed
- (Optional) Telegram Bot Token for Telegram interface

## 📱 Using the Terminal UI

The Terminal UI provides a command-line interface for tracking your work hours locally.

### Quick Start

1. Navigate to the TUI project:
```bash
cd Presentation/Tui
```

2. Run commands directly:
```bash
dotnet run work      # Start working (remote work)
dotnet run status    # View today's status
dotnet run help      # See all commands
```

3. Or use interactive mode:
```bash
dotnet run
# Then type commands interactively:
> work
> lunch
> work
> done
> status
> exit
```

### Available Commands

**Work Tracking:**
- `work` or `working` - Start working (for remote work)
- `commute` or `start` - Start commuting to work
- `atwork` - Arrive at work
- `lunch` - Take lunch break
- `home` - Start commuting home
- `done` or `end` - Finish work day (at home)
- `emergency` - Emergency exit (go home immediately)
- `sickday` - Mark as sick day
- `vacation` - Mark as vacation

**Information:**
- `status` - View today's status and all transitions
- `help` - Show help message

**Control:**
- `exit` or `quit` - Exit interactive mode

### Example Workflows

**Office Work:**
```bash
dotnet run commute   # Start commute
dotnet run atwork    # Arrive at office
dotnet run work      # Start working
dotnet run lunch     # Take lunch
dotnet run work      # Resume work
dotnet run home      # Start commute home
dotnet run done      # Arrive home
```

**Remote Work:**
```bash
dotnet run work      # Start working from home
dotnet run lunch     # Take lunch
dotnet run work      # Resume work
dotnet run done      # Finish work day
```

## 🤖 Using the Telegram Bot

The Telegram Bot provides a mobile-friendly interface for tracking work hours.

### Setup

1. Get a bot token from [@BotFather](https://t.me/BotFather) on Telegram

2. Set the environment variable:
```bash
export TELEGRAM_BOT_TOKEN=your_token_here
```

3. Start the bot:
```bash
cd Presentation/Telegram
dotnet run
```

### Bot Commands

- `/start` - Register or login
- `/commute` - Start commuting to work
- `/atwork` - Arrive at work
- `/work` - Start working
- `/lunch` - Take lunch break
- `/home` - Start commuting home
- `/done` - Finish work day
- `/emergency` - Emergency exit
- `/sickday` - Mark as sick day
- `/vacation` - Mark as vacation
- `/status` - View today's status
- `/help` - Show help message

## 🎯 Features

### State Machine
The app tracks your workday through different states:

**Regular States:**
1. NotStarted → CommutingToWork → AtWork → Working → OnLunch → Working → CommutingHome → AtHome

**Special Features:**
- **Remote Work**: NotStarted → Working → AtHome (skip commute)
- **No Lunch**: Working → CommutingHome (skip lunch)
- **Emergency Exit**: Any state → AtHome
- **Special Days**: SickDay, Vacation, Holiday

### Data Storage
- Terminal UI: SQLite database at `Presentation/Tui/timesheet.db`
- Telegram Bot: SQLite database at `Presentation/Telegram/timesheet.db`
- Each interface maintains its own database

## 🛠️ Development

### Build the Solution
```bash
dotnet build
```

### Run Tests
```bash
dotnet test
```

### Project Structure
```
TimeSheet/
├── Core/
│   ├── Domain/              # Business logic and entities
│   └── Application/         # Use cases and commands
├── Infrastructure/
│   └── Persistence/         # EF Core and repositories
└── Presentation/
    ├── Telegram/            # Telegram bot interface
    └── Tui/                 # Terminal UI interface
```

## 📝 Notes

- All timestamps are stored in UTC
- One WorkDay per user per date
- Transitions must be chronological
- Emergency exits allowed from any state
- Database is created automatically on first run

## 🐛 Troubleshooting

**"TELEGRAM_BOT_TOKEN environment variable is not set"**
- Set the token before running: `export TELEGRAM_BOT_TOKEN=your_token`

**"Invalid state transition"**
- Check the state flow diagram in the specs
- Some transitions are not allowed (e.g., AtHome → Working requires starting a new day)

**Database errors**
- Delete the `timesheet.db` file to start fresh
- Ensure you have write permissions in the directory

## 📚 Documentation

For detailed specifications, see the `specs/` directory:
- [WorkDay State Machine](../specs/workday-state-machine.md)
- [User Management](../specs/user-management.md)
- [Time Tracking](../specs/time-tracking.md)
- [Persistence](../specs/persistence.md)
- [Telegram Bot](../specs/telegram-bot.md)
- [Terminal UI](../specs/terminal-ui.md)
