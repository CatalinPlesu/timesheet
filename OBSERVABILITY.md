# Observability Stack

TimeSheet uses [Seq](https://datalust.co/seq) for structured logging and observability. Seq provides a powerful UI for searching, filtering, and analyzing logs with full support for Serilog's structured logging features.

## Running Seq

### With Docker Compose (Production)

The `compose.yaml` includes a Seq service that runs alongside TimeSheet:

```bash
docker compose up -d
```

Access Seq at: **http://localhost:5341**

The TimeSheet application is configured to send logs to Seq automatically when running in the Docker environment.

**Note**: Seq is configured with `SEQ_FIRSTRUN_NOAUTHENTICATION=true` for local development, which means no login is required to access the UI. This is suitable for local development but should be configured with authentication for production use.

### Local Development

When running the application locally (outside Docker), start Seq separately:

```bash
docker run -d --name seq -p 5341:80 -e ACCEPT_EULA=Y datalust/seq:latest
```

Or use the docker-compose file to start just Seq:

```bash
docker compose up -d seq
```

Access Seq at: **http://localhost:5341**

The application's `appsettings.json` and `appsettings.Development.json` are configured to send logs to `http://localhost:5341` by default.

## Seq Features

### Structured Logging

Seq supports Serilog's structured logging, which means you can:
- Filter logs by properties (not just text search)
- Build queries like `UserId = 123` or `Duration > 1000`
- Create dashboards and alerts based on log properties

### Log Levels

The application uses these log levels:
- **Debug**: Detailed information for diagnosing issues (Development only)
- **Information**: General informational messages
- **Warning**: Potentially harmful situations
- **Error**: Error events that might still allow the application to continue
- **Fatal**: Very severe error events that will presumably lead the application to abort

### Searching Logs

In the Seq UI:
- **Text search**: Just type in the search box
- **Property filters**: Use syntax like `Level = 'Error'` or `SourceContext = 'TimeSheet.Presentation.Telegram.Workers.Worker'`
- **Time range**: Select from the time picker at the top
- **SQL queries**: Click "Show Query" for advanced SQL-like queries

### Common Queries

Find errors:
```
Level = 'Error' or Level = 'Fatal'
```

Find logs from a specific worker:
```
SourceContext like '%Worker%'
```

Find slow operations (if duration is logged):
```
Duration > 1000
```

## Configuration

### Serilog Configuration

The application is configured via `appsettings.json`:

```json
{
  "Serilog": {
    "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.File", "Serilog.Sinks.Seq" ],
    "WriteTo": [
      {
        "Name": "Seq",
        "Args": {
          "serverUrl": "http://localhost:5341"
        }
      }
    ]
  }
}
```

### Docker Environment Variables

When running in Docker, the Seq URL is configured via environment variables in `compose.yaml`:

```yaml
environment:
  - Serilog__WriteTo__1__Name=Seq
  - Serilog__WriteTo__1__Args__serverUrl=http://seq:80
```

The `serverUrl` uses the internal Docker network name `seq` since the containers communicate directly.

## Data Persistence

Seq data is persisted in a Docker volume named `seq-data`. To clear all logs:

```bash
docker compose down -v  # Warning: This deletes ALL data including TimeSheet database
```

Or remove just the Seq volume:

```bash
docker volume rm timesheet_seq-data
```

## Seq License

Seq is free for development use with some limitations:
- Single user
- No authentication required (suitable for local development)
- No retention policies

For production use with multiple users or advanced features, see [Seq licensing](https://datalust.co/pricing).

## Alternatives Considered

- **Jaeger + OpenTelemetry**: Better for distributed tracing, but overkill for a single-service bot
- **Grafana Loki**: Good for log aggregation, but requires more setup (Promtail agent, Grafana)
- **ELK Stack**: Powerful but very heavyweight for this use case

Seq was chosen because:
- Native Serilog integration (via `Serilog.Sinks.Seq`)
- Lightweight single-container deployment
- Excellent structured logging UI
- Free for development use
- Perfect for .NET applications
