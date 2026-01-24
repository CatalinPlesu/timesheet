# TimeSheet Development Agents

This file contains comprehensive development workflows, automation scripts, and tool configurations for the TimeSheet project. These agents help streamline development, testing, deployment, and maintenance workflows.

---

## Development Agents

### 1. Code Quality Agent

**Purpose**: Maintain code quality through static analysis, formatting, and linting.

**Scripts**:
```bash
# Format all C# code
dotnet format

# Run static analysis
dotnet analyze

# Check code coverage
dotnet test --collect:"XPlat Code Coverage" --configuration Release

# Generate coverage report
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:coverage.xml -targetdir:coverage-report -reporttypes:Html
```

**Configuration**:
```json
// .editorconfig
root = true

[*]
charset = utf-8
end_of_line = lf
insert_final_newline = true
indent_style = space
indent_size = 4
trim_trailing_whitespace = true

[*.cs]
indent_size = 4

[*.csproj]
indent_size = 2
```

### 2. Build Agent

**Purpose**: Build and compile the project with optimization and packaging.

**Scripts**:
```bash
# Clean build
dotnet clean -c Release

# Restore dependencies
dotnet restore

# Build solution
dotnet build -c Release --no-restore

# Publish for different platforms
dotnet publish -c Release -o ./publish/Tui --runtime win-x64 --self-contained false
dotnet publish -c Release -o ./publish/Telegram --runtime linux-x64 --self-contained false
```

**Environment Variables**:
- `BUILD_CONFIGURATION`: Release/Debug (default: Release)
- `TARGET_RUNTIME`: Target runtime (default: win-x64)
- `PUBLISH_OUTPUT`: Output directory (default: ./publish)

### 3. Test Agent

**Purpose**: Execute comprehensive testing across all layers.

**Scripts**:
```bash
# Run all tests
dotnet test

# Run specific project tests
dotnet test ./Core/Domain/Domain.csproj

# Run tests with specific filter
dotnet test --filter "TestCategory=Integration"

# Run tests with parallel execution
dotnet test --logger "console;verbosity=detailed" --collect:"XPlat Code Coverage"

# Run tests with CI optimizations
dotnet test --configuration Release --no-build --verbosity normal
```

**Test Categories**:
- `Unit`: Unit tests for domain and application logic
- `Integration`: Integration tests with repositories and services
- `E2E`: End-to-end tests for complete workflows
- `Performance`: Performance and load testing

### 4. Database Agent

**Purpose**: Manage database schema, migrations, and seed data.

**Scripts**:
```bash
# Create new migration
dotnet ef migrations add MigrationName --project Infrastructure/Persistence/Persistence.csproj

# Apply migrations
dotnet ef database update --project Infrastructure/Persistence/Persistence.csproj

# Remove last migration
dotnet ef migrations remove --project Infrastructure/Persistence/Persistence.csproj

# Script migration to SQL
dotnet ef migrations script --project Infrastructure/Persistence/Persistence.csproj

# Generate DbContext
dotnet ef dbcontext scaffold "Data Source=timesheet.db" Microsoft.EntityFrameworkCore.Sqlite --output-dir Persistence/Generated
```

**Database Configuration**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=timesheet.db"
  },
  "Database": {
    "BackupPath": "./backups/",
    "AutoBackup": true,
    "BackupIntervalHours": 24,
    "MaxBackups": 7
  }
}
```

### 5. Security Agent

**Purpose**: Ensure code security, vulnerability scanning, and secret management.

**Scripts**:
```bash
# Run security analysis
dotnet tool install -g security-scan
security-scan --project ./Core/Domain/Domain.csproj

# Check for vulnerabilities in dependencies
dotnet tool install -g dotnet-outdated
dotnet outdated --security-warnings

# Scan for secrets in code
dotnet tool install -g git-secrets
git-secrets --scan

# Generate secure configuration
dotnet user-secrets init
dotnet user-secrets set "Telegram:BotToken" "your-secure-token"
```

**Security Best Practices**:
- Use environment variables for sensitive data
- Implement proper secret rotation
- Enable HTTPS for all communications
- Use parameterized queries to prevent SQL injection
- Implement proper authentication and authorization

### 6. Documentation Agent

**Purpose**: Generate and maintain project documentation.

**Scripts**:
```bash
# Generate API documentation
dotnet tool install -g docfx
docfx metadata
docfx build

# Generate code documentation
dotnet tool install -g markdownlint-cli
markdownlint specs/**/*.md

# Generate architecture diagrams
# (Install PlantUML and configure)
plantuml specs/**/*.md -o docs/diagrams/

# Update README with build status
dotnet tool install -g git-issues
git-issues --owner owner --repo repo --milestone "Documentation"
```

### 7. Deployment Agent

**Purpose**: Handle deployment to different environments and platforms.

**Scripts**:
```bash
# Build and deploy to Docker
docker build -f Dockerfile -t timesheet:latest .
docker run -d -p 8080:80 --name timesheet timesheet:latest

# Deploy to Azure
az webapp up --name timesheet-azure --resource-group myResourceGroup --plan myAppServicePlan

# Deploy to local Kubernetes
kubectl apply -f k8s/deployment.yaml
kubectl apply -f k8s/service.yaml

# Create release package
./scripts/create-release-package.sh

# Deploy to production
./scripts/deploy-production.sh
```

**Deployment Configuration**:
```yaml
# k8s/deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: timesheet
spec:
  replicas: 3
  selector:
    matchLabels:
      app: timesheet
  template:
    metadata:
      labels:
        app: timesheet
    spec:
      containers:
      - name: timesheet
        image: timesheet:latest
        ports:
        - containerPort: 80
```

### 8. Monitoring Agent

**Purpose**: Monitor application health, performance, and errors.

**Scripts**:
```bash
# Run health checks
curl -f http://localhost:8080/health || exit 1

# Collect application metrics
dotnet-counters monitor --name Timesheet --interval 5

# Log aggregation and analysis
./scripts/analyze-logs.sh

# Performance profiling
dotnet-trace collect --profile cpu --process-id 1234

# Memory dump analysis
dotnet-dump collect --process-id 1234
```

**Monitoring Configuration**:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "Metrics": {
    "Enabled": true,
    "IntervalSeconds": 30,
    "Exporters": ["Prometheus", "InfluxDB"]
  }
}
```

### 9. CI/CD Agent

**Purpose**: Continuous Integration and Continuous Deployment pipeline.

**GitHub Actions Workflow**:
```yaml
# .github/workflows/ci.yml
name: CI/CD Pipeline

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    - name: Restore dependencies
      run: dotnet restore
    - name: Build
      run: dotnet build --no-restore
    - name: Test
      run: dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage"
    - name: Upload coverage
      uses: codecov/codecov-action@v3

  security:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    - name: Security scan
      run: |
        dotnet tool install -g security-scan
        security-scan --project ./Core/Domain/Domain.csproj

  deploy:
    needs: [test, security]
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    steps:
    - uses: actions/checkout@v3
    - name: Deploy to production
      run: ./scripts/deploy-production.sh
```

### 10. Local Development Agent

**Purpose**: Streamline local development setup and workflows.

**Setup Scripts**:
```bash
# Development environment setup
./scripts/setup-dev-env.sh

# Start all services in Docker Compose
docker-compose up -d

# Run development server
dotnet watch run --project Presentation/Tui/Tui.csproj

# Run database migrations
dotnet ef database update

# Generate test data
dotnet run --project TestDataGenerator --configuration Release

# Clean development environment
./scripts/clean-dev-env.sh
```

**Development Tools**:
```json
// .vscode/settings.json
{
  "csharp.format.enable": true,
  "csharp.showOctokitReferences": true,
  "csharp.showReferencesCodeLens": true,
  "dotnet.server.path": "/usr/local/share/dotnet/dotnet",
  "files.associations": {
    "*.csproj": "xml"
  }
}
```

---

## Workflow Automation

### Daily Standup Script
```bash
#!/bin/bash
# scripts/daily-standup.sh

echo "=== Daily Standup ==="
echo "📊 Today's Progress:"
git log --since="yesterday" --pretty=format:"%h %s" --author="$(git config user.name)"

echo ""
echo "🚀 Tasks for Today:"
if [ -f "TODO.md" ]; then
    grep -E "^\* \[ \]" TODO.md | head -5
fi

echo ""
echo "⚠️  Blocked Issues:"
if [ -f "BLOCKERS.md" ]; then
    cat BLOCKERS.md
fi
```

### Code Review Checklist
```bash
#!/bin/bash
# scripts/code-review.sh

echo "=== Code Review Checklist ==="

# Check code formatting
echo "✓ Checking code formatting..."
dotnet format --verify-no-changes || exit 1

# Run tests
echo "✓ Running tests..."
dotnet test --no-build --verbosity normal || exit 1

# Check security
echo "✓ Running security scan..."
dotnet tool install -g security-scan
security-scan --project ./Core/Domain/Domain.csproj || exit 1

# Check documentation
echo "✓ Checking documentation..."
if [ -f "README.md" ]; then
    if ! grep -q "$(git rev-parse --short HEAD)" README.md; then
        echo "⚠️  Current commit not documented in README.md"
    fi
fi

echo "✓ Code review checklist complete"
```

### Performance Testing Script
```bash
#!/bin/bash
# scripts/performance-test.sh

echo "=== Performance Testing ==="

# Build release version
dotnet build -c Release

# Run load test
echo "🔥 Running load test..."
dotnet run --project Performance/LoadTest --configuration Release

# Analyze results
echo "📊 Analyzing results..."
if [ -f "performance-report.html" ]; then
    echo "📈 Performance report generated: performance-report.html"
fi

# Check memory usage
echo "💾 Memory usage analysis..."
dotnet-counters monitor --name Timesheet --interval 5 --duration 30
```

---

## Environment Configuration

### Development Environment
```json
// appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "Database": {
    "Path": "./data/timesheet-dev.db",
    "AutoBackup": false
  },
  "FeatureFlags": {
    "EnableAnalytics": true,
    "EnableNotifications": true,
    "EnableDebugTools": true
  }
}
```

### Production Environment
```json
// appsettings.Production.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "Database": {
    "Path": "/var/lib/timesheet/timesheet.db",
    "AutoBackup": true,
    "BackupPath": "/var/backups/timesheet/"
  },
  "FeatureFlags": {
    "EnableAnalytics": true,
    "EnableNotifications": true,
    "EnableDebugTools": false
  }
}
```

---

## Maintenance Scripts

### Database Maintenance
```bash
#!/bin/bash
# scripts/maintenance-db.sh

echo "=== Database Maintenance ==="

# Create backup
echo "📁 Creating database backup..."
mkdir -p ./backups
cp ./data/timesheet.db ./backups/timesheet-$(date +%Y%m%d-%H%M%S).db

# Optimize database
echo "⚡ Optimizing database..."
sqlite3 ./data/timesheet.db "VACUUM;"
sqlite3 ./data/timesheet.db "ANALYZE;"

# Clean old backups
echo "🧹 Cleaning old backups..."
find ./backups -name "timesheet-*.db" -mtime +7 -delete

echo "✅ Database maintenance complete"
```

### Log Rotation
```bash
#!/bin/bash
# scripts/rotate-logs.sh

echo "=== Log Rotation ==="

# Compress old logs
find ./logs -name "*.log" -mtime +1 -exec gzip {} \;

# Remove logs older than 30 days
find ./logs -name "*.log.gz" -mtime +30 -delete

# Create new log file
touch ./logs/timesheet.log

echo "✅ Log rotation complete"
```

---

## Troubleshooting Guides

### Common Issues
```bash
#!/bin/bash
# scripts/troubleshoot.sh

echo "=== Troubleshooting Guide ==="

# Check database connectivity
echo "🔍 Checking database connectivity..."
if [ ! -f "./data/timesheet.db" ]; then
    echo "❌ Database file not found. Running migrations..."
    dotnet ef database update
else
    echo "✅ Database file exists"
fi

# Check application logs
echo "📄 Checking application logs..."
if [ -f "./logs/timesheet.log" ]; then
    echo "📋 Last 10 lines of logs:"
    tail -10 ./logs/timesheet.log
else
    echo "⚠️  Log file not found"
fi

# Check system resources
echo "💻 System resources..."
df -h .
free -h
echo "Load average: $(uptime | awk -F'load average:' '{ print $2 }')"
```

---

*Last Updated: January 2026*