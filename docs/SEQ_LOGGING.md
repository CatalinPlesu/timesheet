# Seq Logging Setup

This project uses [Seq](https://datalust.co/seq) for centralized structured logging and activity inspection.

## Quick Start

### Local Development

Start Seq container:
```bash
docker run -d --name seq \
  -e ACCEPT_EULA=Y \
  -e SEQ_FIRSTRUN_NOAUTHENTICATION=true \
  -p 5341:80 \
  datalust/seq:latest
```

Access Seq UI at: http://localhost:5341

### Docker Compose (Production)

Seq is already configured in `compose.yaml`:
```bash
docker compose up -d
```

Access Seq UI at: http://localhost:5341 (or port specified by `SEQ_PORT` environment variable)

## Configuration

### Telegram Bot

- **Package**: `Serilog.Sinks.Seq` v9.0.0
- **Config**: `TimeSheet.Presentation.Telegram/appsettings.json`
- **Server URL** (local): `http://localhost:5341`
- **Server URL** (docker): `http://seq:80`

### API

- **Package**: `Serilog.Sinks.Seq` v9.0.0
- **Config**: `TimeSheet.Presentation.API/appsettings.json`
- **Server URL** (local): `http://localhost:5341`
- **Server URL** (docker): `http://seq:80`

## Log Levels

Both projects use the same log level configuration:

- **Default**: Information
- **Microsoft**: Warning
- **Microsoft.Hosting.Lifetime**: Information
- **Microsoft.EntityFrameworkCore**: Warning
- **System**: Warning

## Outputs

Both projects write to 3 sinks:

1. **Console** - Real-time development feedback
2. **File** - Rolling daily logs (`logs/timesheet-.log` or `logs/timesheet-api-.log`)
3. **Seq** - Structured query and analysis

## Querying Logs

### By Source Context
```
SourceContext like 'TimeSheet%'
```

### By Level
```
@Level = 'Error'
```

### By Time Range
```
@Timestamp >= Now() - 1h
```

### Telegram-specific
```
SourceContext like 'TimeSheet.Presentation.Telegram%'
```

### API-specific
```
SourceContext like 'TimeSheet.Presentation.API%'
```

## Troubleshooting

### Logs not appearing in Seq

1. **Check Seq is running**:
   ```bash
   docker ps | grep seq
   ```

2. **Check connectivity**:
   ```bash
   curl http://localhost:5341/api
   ```

3. **Check configuration**:
   - Verify `Serilog.WriteTo` section in `appsettings.json`
   - Ensure `serverUrl` matches your Seq instance

4. **Check log levels**:
   - Ensure minimum level is not too restrictive
   - Check override levels for specific namespaces

### Container networking issues

In Docker Compose, services use the internal hostname `seq` instead of `localhost`:
- Bot: `http://seq:80`
- API: `http://seq:80`
- Host access: `http://localhost:5341`

## Environment Variables

### Docker Compose Override

Override Seq configuration via environment variables:

**Telegram Bot:**
```yaml
- Serilog__WriteTo__1__Name=Seq
- Serilog__WriteTo__1__Args__serverUrl=http://seq:80
```

**API:**
```yaml
- Serilog__WriteTo__2__Name=Seq
- Serilog__WriteTo__2__Args__serverUrl=http://seq:80
```

### Custom Seq Port

```bash
SEQ_PORT=8080 docker compose up -d
```

Then access Seq at: http://localhost:8080
