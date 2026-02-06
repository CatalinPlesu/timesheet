# TimeSheet.Presentation.Telegram

Telegram bot presentation layer for the TimeSheet application.

## Configuration

The bot requires a Telegram bot token to run. You can configure this in several ways:

### Option 1: User Secrets (Recommended for Development)

```bash
cd TimeSheet.Presentation.Telegram
dotnet user-secrets set "Bot:Token" "YOUR_BOT_TOKEN_HERE"
```

### Option 2: Environment Variable

```bash
export Bot__Token="YOUR_BOT_TOKEN_HERE"
dotnet run
```

### Option 3: appsettings.Development.json (Not Recommended)

Edit `appsettings.Development.json` and set the token:

```json
{
  "Bot": {
    "Token": "YOUR_BOT_TOKEN_HERE"
  }
}
```

**Warning**: Never commit your bot token to source control.

## Getting a Bot Token

1. Open Telegram and search for [@BotFather](https://t.me/botfather)
2. Send `/newbot` and follow the instructions
3. Copy the token provided by BotFather
4. Configure the token using one of the methods above

## Running the Bot

```bash
dotnet run
```

The bot will start polling for updates. You should see a log message:

```
Telegram bot started: @YourBotUsername (ID: 123456789)
```

## Current Features

- Polling-based update receiver
- Basic logging of incoming messages
- Graceful shutdown handling

Command processing will be implemented in Epic 2 (Base Time Tracking).
