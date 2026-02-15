# Docker Deployment Guide

This guide explains how to run the TimeSheet application using Docker Compose.

## Architecture

The Docker Compose setup includes 4 services:

1. **telegram-bot** - Telegram bot for time tracking commands
2. **api** - REST API for web frontend
3. **frontend** - SvelteKit web application served by nginx
4. **seq** - Centralized logging and diagnostics

All services share a single SQLite database via volume mount.

## Quick Start

### 1. Configure Environment

Copy the example environment file and edit with your values:

```bash
cp .env.example .env
```

Required variables:
- `BOT_TOKEN` - Your Telegram bot token from @BotFather
- `JWT_SECRET_KEY` - Random 32+ character string for JWT signing

### 2. Build and Run

```bash
docker-compose up --build
```

Services will be available at:
- Frontend: http://localhost:3000
- API: http://localhost:5000
- Seq Logs: http://localhost:5341

### 3. Stop Services

```bash
docker-compose down
```

To remove volumes (including database):
```bash
docker-compose down -v
```

## Production Deployment

For production use:

1. **Update environment variables:**
   - Set a secure `JWT_SECRET_KEY` (use a password generator)
   - Set `VITE_API_URL` to your production API URL
   - Configure `CORS__AllowedOrigins__1` for your frontend domain

2. **Use TLS/HTTPS:**
   - Add a reverse proxy (nginx, Traefik, Caddy) in front of the services
   - Obtain SSL certificates (Let's Encrypt)

3. **Database persistence:**
   - Ensure `./data` directory has proper permissions
   - Consider regular backups of `./data/timesheet.db`

4. **Resource limits:**
   - Add memory and CPU limits to docker-compose.yml if needed

## Environment Variables

### Required

| Variable | Description | Example |
|----------|-------------|---------|
| `BOT_TOKEN` | Telegram bot token | `123456:ABC-DEF...` |
| `JWT_SECRET_KEY` | JWT signing key | `your-random-32-char-key` |

### Optional (with defaults)

| Variable | Default | Description |
|----------|---------|-------------|
| `API_PORT` | `5000` | API port on host |
| `FRONTEND_PORT` | `3000` | Frontend port on host |
| `SEQ_PORT` | `5341` | Seq logs port on host |
| `VITE_API_URL` | `http://localhost:5000` | API URL for frontend (build-time) |
| `JWT_EXPIRATION_MINUTES` | `60` | JWT token expiration |

## Troubleshooting

### Check service logs

```bash
# All services
docker-compose logs

# Specific service
docker-compose logs api
docker-compose logs frontend
docker-compose logs telegram-bot
```

### Rebuild from scratch

```bash
docker-compose down -v
docker-compose build --no-cache
docker-compose up
```

### Database issues

The database is stored in `./data/timesheet.db`. If you need to reset:

```bash
docker-compose down
rm -rf ./data
docker-compose up
```

### Port conflicts

If ports are already in use, change them in `.env`:

```env
API_PORT=5001
FRONTEND_PORT=3001
SEQ_PORT=5342
```

## Development vs Production

### Development

The current setup uses:
- `docker-compose.yml` - Local builds from source
- Seq logging available at http://localhost:5341
- No authentication required for Seq

### Production (Pre-built Images)

For using pre-built images:

```bash
docker-compose -f compose.yaml.prebuilt up
```

This uses images from GitHub Container Registry instead of building locally.

## File Structure

```
.
├── docker-compose.yml              # Main compose file
├── compose.yaml.prebuilt           # Compose file for pre-built images
├── .env.example                    # Environment template
├── .dockerignore                   # Exclude from build context
├── data/                           # SQLite database (created on first run)
│   └── timesheet.db
├── TimeSheet.Presentation.Telegram/
│   └── Dockerfile                  # Bot Dockerfile
├── TimeSheet.Presentation.API/
│   └── Dockerfile                  # API Dockerfile
└── TimeSheet.Frontend/
    ├── Dockerfile                  # Frontend Dockerfile
    ├── nginx.conf                  # Nginx configuration
    └── .dockerignore               # Frontend exclusions
```

## Health Checks

All services have health checks configured:

- **API**: HTTP check on `/health` endpoint
- **Frontend**: HTTP check on `/`
- **Bot**: Process check (pgrep)

View health status:

```bash
docker-compose ps
```
