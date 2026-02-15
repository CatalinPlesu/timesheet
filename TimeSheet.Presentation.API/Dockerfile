# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy all source files
COPY . .

# Build and publish
WORKDIR /src/TimeSheet.Presentation.API
RUN dotnet publish TimeSheet.Presentation.API.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Copy published output
COPY --from=build /app/publish .

# Create data directory for SQLite
RUN mkdir -p /app/data

# Set environment variables
ENV DOTNET_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5000
ENV Database__ConnectionString="Data Source=/app/data/timesheet.db"

# Expose API port
EXPOSE 5000

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:5000/health || exit 1

ENTRYPOINT ["dotnet", "TimeSheet.Presentation.API.dll"]
