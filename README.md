# TimeSheet

A private Telegram bot for tracking your own work hours — so work doesn't quietly take more of your day than it should.

See [TECHNICAL.md](TECHNICAL.md) for full feature docs, architecture, and development setup.

## Deploy

Copy `compose.yaml` to your server alongside a `.env` file:

```env
BOT_TOKEN=123456789:ABCdef...
JWT_SECRET_KEY=<openssl rand -base64 32>
FRONTEND_EXTERNAL_URL=https://timesheet.example.com
API_PORT=5191
FRONTEND_PORT=3345
```

Then start the stack:

```bash
mkdir -p data
podman compose up -d
```

### Caddyfile

```
timesheet.example.com {
    handle /api/* {
        reverse_proxy 127.0.0.1:5191
    }

    handle {
        reverse_proxy 127.0.0.1:3345
    }
}
```

## License

MIT — see [LICENSE](LICENSE).
