# TimeSheet Bot

A private Telegram bot for personal work-hour tracking. Not employer surveillance — the goal is to help you ensure work doesn't take more time than it should.

## ✨ Features

### 🔄 Time Tracking
- **`/commute`** (`/c`) - Track commute to work or to home
- **`/work`** (`/w`) - Track work time  
- **`/lunch`** (`/l`) - Track lunch break

### 📊 Reports & Analytics
- **`/report`** - Generate visual reports with charts and graphs
  - Daily breakdown with bar charts
  - Work hours trend analysis
  - Activity breakdown (work, commute, lunch)
  - Daily averages comparison
  - Commute patterns by day of week
- **`/table`** - View data in formatted table format

### ✏️ Editing & Corrections
- **`/edit`** - Edit the most recent entry (or N entries back)
  - Inline keyboard with adjustment buttons: `-30m` `-5m` `-1m` `+1m` `+5m` `+30m`
- **`/delete`** - Delete an entry with confirmation prompt

### 📋 Management
- **`/status`** - Show current tracking status
- **`/list`** - List recent tracking entries
- **`/settings`** - Configure user preferences
- **`/help`** - Show available commands

## 🚀 Getting Started

### 1. Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Telegram account
- Bot token from [@BotFather](https://t.me/botfather)

### 2. Setup
```bash
# Clone the repository
git clone <repository-url>
cd TimeSheet

# Restore dependencies
dotnet restore

# Build the solution
dotnet build
```

### 3. Configuration
The bot requires a Telegram bot token. Configure it using one of these methods:

#### Option 1: User Secrets (Recommended for Development)
```bash
cd TimeSheet.Presentation.Telegram
dotnet user-secrets set "Bot:Token" "YOUR_BOT_TOKEN_HERE"
```

#### Option 2: Environment Variable
```bash
export Bot__Token="YOUR_BOT_TOKEN_HERE"
dotnet run
```

#### Option 3: appsettings.json
Edit `TimeSheet.Presentation.Telegram/appsettings.Development.json`:
```json
{
  "Bot": {
    "Token": "YOUR_BOT_TOKEN_HERE"
  }
}
```

**Warning**: Never commit your bot token to source control.

### 4. Registration
1. Start the bot: `dotnet run`
2. The console will display a registration command: `/register [24-word mnemonic]`
3. Send this command to the bot to register as the first user (admin)
4. For additional users, generate new mnemonics and share them privately

## 💡 How to Use

### Basic Workflow
1. **Start your day**: `/commute` - Tracks your commute to work
2. **Start working**: `/work` - Tracks your work time
3. **Take lunch**: `/lunch` - Tracks your lunch break
4. **Resume work**: `/work` - After lunch break
5. **Go home**: `/commute` - Tracks your commute home
6. **End day**: `/commute` again to stop commute tracking

### Command Behavior
- **Toggle behavior**: Commands act as toggles
  - Starting a new state stops the previous one
  - Repeating the same command stops it
- **Commute direction**: Automatically detects commute-to-work vs commute-to-home based on context
- **Multiple work sessions**: Supported (split by lunch, etc.)

### Time Parameters
Commands support optional time parameters:
- `-m` / `+m` — action started m minutes ago / will start m minutes from now
- `[hh:mm]` — exact start time in 24h format

Examples:
- `/work -15m` - Started working 15 minutes ago
- `/lunch +10m` - Will start lunch break in 10 minutes
- `/work 09:00` - Started working at 9:00 AM

### Editing Entries
1. `/edit` - Edit the most recent entry
2. `/edit 1` - Edit 1 entry back
3. `/edit 2` - Edit 2 entries back
4. Use inline keyboard buttons for quick adjustments

### Viewing Reports
- `/report` - Generate visual reports with charts
- `/table` - View data in formatted table format
- Reports include daily breakdowns, trends, and commute patterns

## 📊 Report Examples

The bot generates various types of visual reports:

### Daily Breakdown Chart
Shows work hours by day in a clean bar chart format.

### Work Hours Trend
Line chart tracking work hours over time with trend analysis.

### Activity Breakdown
Grouped bar chart comparing work, commute, and lunch time.

### Commute Patterns
Analysis of commute times by day of week to help optimize travel.

## 🔧 Configuration

### User Settings
Configure via `/settings` command:
- **UTC offset** - Timezone for accurate tracking
- **Auto-shutdown** - Automatically close forgotten states (absolute time or percentage-based)

### Background Workers
The bot includes several background workers:
- **AutoShutdownWorker** - Automatically closes forgotten tracking sessions
- **ForgotShutdownWorker** - Alerts about sessions that might have been forgotten
- **LunchReminderWorker** - Reminds to take lunch breaks
- **WorkHoursAlertWorker** - Alerts when reaching daily work hour limits

## 🏗️ Architecture

This is a **Clean Architecture** application with .NET 10 / C# 14, featuring:

- **Domain Layer** - Business logic and entities
- **Application Layer** - Use cases and services
- **Infrastructure Layer** - Data persistence (SQLite) and external integrations
- **Presentation Layer** - Telegram bot interface

### Key Technologies
- **.NET 10** - Modern C# with latest features
- **SQLite** - Local database for data persistence
- **ScottPlot** - Chart generation for visual reports
- **Serilog** - Structured logging
- **Telegram.Bot** - Telegram API integration

## 🧪 Testing

The project includes comprehensive test coverage:

- **Unit Tests** - Core logic and services
- **Integration Tests** - Telegram bot functionality
- **Test Coverage** - Mocks and fixtures for reliable testing

Run tests:
```bash
dotnet test
```

## 📝 Development

### Project Structure
```
TimeSheet/
├── TimeSheet.Core.Domain/          # Domain entities and business logic
├── TimeSheet.Core.Application/     # Use cases and application services
├── TimeSheet.Infrastructure.Persistence/ # Data persistence
├── TimeSheet.Presentation.Telegram/ # Telegram bot interface
├── TimeSheet.Tests.Unit/           # Unit tests
└── TimeSheet.Tests.Integration/    # Integration tests
```

### Issue Tracking
This project uses **bd** (beads) for issue tracking. Available commands:
- `bd ready` - Find available work
- `bd show <id>` - View issue details
- `bd close <id>` - Complete work
- `bd sync` - Sync with git

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Ensure all tests pass
6. Submit a pull request

## 📄 License

This project is private for personal use.

## 🆘 Support

For issues and questions:
1. Check existing issues in the bd tracker
2. Create a new issue if needed
3. Provide detailed description of the problem

---

**Remember**: This bot is designed for personal productivity, not employer surveillance. Use it to maintain a healthy work-life balance! 🎯