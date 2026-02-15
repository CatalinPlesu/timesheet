# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy all source files
COPY . .

# Build and publish
WORKDIR /src/TimeSheet.Presentation.Telegram
RUN dotnet publish TimeSheet.Presentation.Telegram.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Copy published output
COPY --from=build /app/publish .

# Create data directory for SQLite
RUN mkdir -p /app/data

# Set environment variables
ENV DOTNET_ENVIRONMENT=Production
ENV Database__ConnectionString="Data Source=/app/data/timesheet.db"

# Health check (bot doesn't have HTTP endpoint, check process)
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD pgrep -f TimeSheet.Presentation.Telegram || exit 1

ENTRYPOINT ["dotnet", "TimeSheet.Presentation.Telegram.dll"]
