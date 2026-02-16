# dotnet-just — a just + fzf workflow for .NET multi-project solutions
# Prerequisites: just, fzf, dotnet, dotnet-ef (global tool for EF commands)
# Works out of the box with auto-detection. Run `just setup` to cache choices.

# ─── configuration ───────────────────────────────────────────────────────────
_db := "dotnet-just"
_key-root := replace(env("PWD"), "/", "__") + "-root"

# Show available commands
default:
    @just --list

# Show auto-detected/cached configuration
info:
    @ROOT=$(skate get "{{_key-root}}@{{_db}}" 2>/dev/null); \
    if [ -n "$ROOT" ]; then \
      echo "Root (cached):     $ROOT"; \
    else \
      DIR="$PWD"; while [ "$DIR" != "/" ]; do if ls "$DIR"/*.sln "$DIR"/*.slnx 2>/dev/null | head -n1 >/dev/null 2>&1; then ROOT="$DIR"; break; fi; DIR=$(dirname "$DIR"); done; \
      if [ -z "$ROOT" ]; then DIR="$PWD"; while [ "$DIR" != "/" ]; do if ls "$DIR"/*.csproj 2>/dev/null | head -n1 >/dev/null 2>&1; then ROOT="$DIR"; break; fi; DIR=$(dirname "$DIR"); done; fi; \
      echo "Root (detected):   $ROOT"; \
    fi; \
    if [ -n "$ROOT" ]; then \
      RKEY=$(echo "$ROOT" | sed 's|/|__|g'); \
      STARTUP=$(skate get "${RKEY}-startup@{{_db}}" 2>/dev/null); \
      DATA=$(skate get "${RKEY}-data@{{_db}}" 2>/dev/null); \
      TEST=$(skate get "${RKEY}-test@{{_db}}" 2>/dev/null); \
      echo "Startup project:   ${STARTUP:-(auto-detect)}"; \
      echo "Data project:      ${DATA:-(auto-detect)}"; \
      echo "Test project:      ${TEST:-(auto-detect)}"; \
      STACK=$(skate get "${RKEY}-mig-stack@{{_db}}" 2>/dev/null); \
      if [ -n "$STACK" ]; then \
        echo ""; \
        echo "Migration stack:"; \
        echo "$STACK" | sed 's/^/  /'; \
      fi; \
    fi

# Cache project selections (run this first in a new project)
setup:
    @echo "═══ Finding solution root …"; \
    DIR="$PWD"; ROOT=""; \
    while [ "$DIR" != "/" ]; do if ls "$DIR"/*.sln "$DIR"/*.slnx 2>/dev/null | head -n1 >/dev/null 2>&1; then ROOT="$DIR"; break; fi; DIR=$(dirname "$DIR"); done; \
    if [ -z "$ROOT" ]; then DIR="$PWD"; while [ "$DIR" != "/" ]; do if ls "$DIR"/*.csproj 2>/dev/null | head -n1 >/dev/null 2>&1; then ROOT="$DIR"; break; fi; DIR=$(dirname "$DIR"); done; fi; \
    if [ -z "$ROOT" ]; then echo "✗ Could not find any .sln, .slnx, or .csproj"; exit 1; fi; \
    echo "  ✓ root → $ROOT"; \
    skate set "{{_key-root}}@{{_db}}" "$ROOT"; \
    RKEY=$(echo "$ROOT" | sed 's|/|__|g'); \
    echo ""; \
    PROJECTS=$(find "$ROOT" -name "*.csproj" | sort); \
    if [ -z "$PROJECTS" ]; then echo "✗ No .csproj files found"; exit 1; fi; \
    echo "═══ Select projects (Esc to skip test) …"; \
    STARTUP=$(echo "$PROJECTS" | fzf --prompt="Startup project> " --no-multi) || { echo "✗ Aborted."; exit 1; }; \
    skate set "${RKEY}-startup@{{_db}}" "$STARTUP"; \
    echo "  ✓ startup → $STARTUP"; \
    DATA=$(echo "$PROJECTS" | fzf --prompt="Data project>    " --no-multi) || { echo "✗ Aborted."; exit 1; }; \
    skate set "${RKEY}-data@{{_db}}" "$DATA"; \
    echo "  ✓ data    → $DATA"; \
    TEST=$(echo "$PROJECTS" | fzf --prompt="Test project>    " --no-multi) || true; \
    if [ -n "$TEST" ]; then skate set "${RKEY}-test@{{_db}}" "$TEST"; echo "  ✓ test    → $TEST"; else echo "  ⊘ test    (skipped)"; fi; \
    echo ""; \
    echo "═══ Done. Configuration cached."

# Clear cached configuration
clear:
    @ROOT=$(skate get "{{_key-root}}@{{_db}}" 2>/dev/null); \
    if [ -n "$ROOT" ]; then \
      RKEY=$(echo "$ROOT" | sed 's|/|__|g'); \
      skate delete "{{_key-root}}@{{_db}}" 2>/dev/null || true; \
      skate delete "${RKEY}-startup@{{_db}}" 2>/dev/null || true; \
      skate delete "${RKEY}-data@{{_db}}" 2>/dev/null || true; \
      skate delete "${RKEY}-test@{{_db}}" 2>/dev/null || true; \
      skate delete "${RKEY}-mig-stack@{{_db}}" 2>/dev/null || true; \
      echo "✓ Cache cleared."; \
    else \
      echo "(nothing cached)"; \
    fi

# Run the startup project (auto-detected or cached)
run *args:
    @ROOT=$(skate get "{{_key-root}}@{{_db}}" 2>/dev/null); \
    if [ -z "$ROOT" ]; then DIR="$PWD"; while [ "$DIR" != "/" ]; do if ls "$DIR"/*.sln "$DIR"/*.slnx 2>/dev/null | head -n1 >/dev/null 2>&1; then ROOT="$DIR"; break; fi; DIR=$(dirname "$DIR"); done; if [ -z "$ROOT" ]; then DIR="$PWD"; while [ "$DIR" != "/" ]; do if ls "$DIR"/*.csproj 2>/dev/null | head -n1 >/dev/null 2>&1; then ROOT="$DIR"; break; fi; DIR=$(dirname "$DIR"); done; fi; fi; \
    RKEY=$(echo "$ROOT" | sed 's|/|__|g'); \
    STARTUP=$(skate get "${RKEY}-startup@{{_db}}" 2>/dev/null); \
    if [ -z "$STARTUP" ]; then \
      PROJS=$(find "$ROOT" -name "*.csproj" | sort); \
      for proj in $PROJS; do if grep -q '<OutputType>Exe</OutputType>' "$proj" 2>/dev/null || grep -q '<OutputType>WinExe</OutputType>' "$proj" 2>/dev/null; then STARTUP="$proj"; break; fi; done; \
      if [ -z "$STARTUP" ]; then for proj in $PROJS; do PROJDIR=$(dirname "$proj"); if [ -f "$PROJDIR/Program.cs" ]; then STARTUP="$proj"; break; fi; done; fi; \
      if [ -z "$STARTUP" ]; then STARTUP=$(echo "$PROJS" | head -n1); fi; \
    fi; \
    if [ -z "$STARTUP" ]; then echo "✗ No startup project found"; exit 1; fi; \
    echo "→ Running: $STARTUP"; \
    dotnet run --project "$STARTUP" {{args}}

# Build the solution or a specific project
build proj="":
    @if [ -n "{{proj}}" ]; then dotnet build "{{proj}}"; else \
      ROOT=$(skate get "{{_key-root}}@{{_db}}" 2>/dev/null); \
      if [ -z "$ROOT" ]; then DIR="$PWD"; while [ "$DIR" != "/" ]; do if ls "$DIR"/*.sln "$DIR"/*.slnx 2>/dev/null | head -n1 >/dev/null 2>&1; then ROOT="$DIR"; break; fi; DIR=$(dirname "$DIR"); done; if [ -z "$ROOT" ]; then DIR="$PWD"; while [ "$DIR" != "/" ]; do if ls "$DIR"/*.csproj 2>/dev/null | head -n1 >/dev/null 2>&1; then ROOT="$DIR"; break; fi; DIR=$(dirname "$DIR"); done; fi; fi; \
      SLN=$(ls "$ROOT"/*.slnx "$ROOT"/*.sln 2>/dev/null | head -n1); \
      if [ -n "$SLN" ]; then dotnet build "$SLN"; else echo "✗ No solution found. Try: just build <project.csproj>"; exit 1; fi; \
    fi

# Run all tests (unit + integration)
test *args:
    @ROOT=$(skate get "{{_key-root}}@{{_db}}" 2>/dev/null); \
    if [ -z "$ROOT" ]; then DIR="$PWD"; while [ "$DIR" != "/" ]; do if ls "$DIR"/*.sln "$DIR"/*.slnx 2>/dev/null | head -n1 >/dev/null 2>&1; then ROOT="$DIR"; break; fi; DIR=$(dirname "$DIR"); done; if [ -z "$ROOT" ]; then DIR="$PWD"; while [ "$DIR" != "/" ]; do if ls "$DIR"/*.csproj 2>/dev/null | head -n1 >/dev/null 2>&1; then ROOT="$DIR"; break; fi; DIR=$(dirname "$DIR"); done; fi; fi; \
    SLN=$(ls "$ROOT"/*.slnx "$ROOT"/*.sln 2>/dev/null | head -n1); \
    if [ -n "$SLN" ]; then dotnet test "$SLN" {{args}}; else echo "✗ No solution found."; exit 1; fi

# Run only unit tests
test-unit *args:
    @ROOT=$(skate get "{{_key-root}}@{{_db}}" 2>/dev/null); \
    if [ -z "$ROOT" ]; then DIR="$PWD"; while [ "$DIR" != "/" ]; do if ls "$DIR"/*.sln "$DIR"/*.slnx 2>/dev/null | head -n1 >/dev/null 2>&1; then ROOT="$DIR"; break; fi; DIR=$(dirname "$DIR"); done; if [ -z "$ROOT" ]; then DIR="$PWD"; while [ "$DIR" != "/" ]; do if ls "$DIR"/*.csproj 2>/dev/null | head -n1 >/dev/null 2>&1; then ROOT="$DIR"; break; fi; DIR=$(dirname "$DIR"); done; fi; fi; \
    UNIT=$(find "$ROOT" -name "*.Tests.Unit.csproj" | head -n1); \
    if [ -z "$UNIT" ]; then echo "✗ No unit test project found."; exit 1; fi; \
    dotnet test "$UNIT" {{args}}

# Run only integration tests
test-integration *args:
    @ROOT=$(skate get "{{_key-root}}@{{_db}}" 2>/dev/null); \
    if [ -z "$ROOT" ]; then DIR="$PWD"; while [ "$DIR" != "/" ]; do if ls "$DIR"/*.sln "$DIR"/*.slnx 2>/dev/null | head -n1 >/dev/null 2>&1; then ROOT="$DIR"; break; fi; DIR=$(dirname "$DIR"); done; if [ -z "$ROOT" ]; then DIR="$PWD"; while [ "$DIR" != "/" ]; do if ls "$DIR"/*.csproj 2>/dev/null | head -n1 >/dev/null 2>&1; then ROOT="$DIR"; break; fi; DIR=$(dirname "$DIR"); done; fi; fi; \
    INTEGRATION=$(find "$ROOT" -name "*.Tests.Integration.csproj" | head -n1); \
    if [ -z "$INTEGRATION" ]; then echo "✗ No integration test project found."; exit 1; fi; \
    dotnet test "$INTEGRATION" {{args}}

# Run tests in watch mode
test-watch *args:
    @ROOT=$(skate get "{{_key-root}}@{{_db}}" 2>/dev/null); \
    if [ -z "$ROOT" ]; then DIR="$PWD"; while [ "$DIR" != "/" ]; do if ls "$DIR"/*.sln "$DIR"/*.slnx 2>/dev/null | head -n1 >/dev/null 2>&1; then ROOT="$DIR"; break; fi; DIR=$(dirname "$DIR"); done; if [ -z "$ROOT" ]; then DIR="$PWD"; while [ "$DIR" != "/" ]; do if ls "$DIR"/*.csproj 2>/dev/null | head -n1 >/dev/null 2>&1; then ROOT="$DIR"; break; fi; DIR=$(dirname "$DIR"); done; fi; fi; \
    SLN=$(ls "$ROOT"/*.slnx "$ROOT"/*.sln 2>/dev/null | head -n1); \
    if [ -n "$SLN" ]; then dotnet watch test "$SLN" {{args}}; else echo "✗ No solution found."; exit 1; fi

# Run the startup project in watch mode
watch *args:
    @ROOT=$(skate get "{{_key-root}}@{{_db}}" 2>/dev/null); \
    if [ -z "$ROOT" ]; then DIR="$PWD"; while [ "$DIR" != "/" ]; do if ls "$DIR"/*.sln "$DIR"/*.slnx 2>/dev/null | head -n1 >/dev/null 2>&1; then ROOT="$DIR"; break; fi; DIR=$(dirname "$DIR"); done; if [ -z "$ROOT" ]; then DIR="$PWD"; while [ "$DIR" != "/" ]; do if ls "$DIR"/*.csproj 2>/dev/null | head -n1 >/dev/null 2>&1; then ROOT="$DIR"; break; fi; DIR=$(dirname "$DIR"); done; fi; fi; \
    RKEY=$(echo "$ROOT" | sed 's|/|__|g'); \
    STARTUP=$(skate get "${RKEY}-startup@{{_db}}" 2>/dev/null); \
    if [ -z "$STARTUP" ]; then \
      PROJS=$(find "$ROOT" -name "*.csproj" | sort); \
      for proj in $PROJS; do if grep -q '<OutputType>Exe</OutputType>' "$proj" 2>/dev/null || grep -q '<OutputType>WinExe</OutputType>' "$proj" 2>/dev/null; then STARTUP="$proj"; break; fi; done; \
      if [ -z "$STARTUP" ]; then for proj in $PROJS; do PROJDIR=$(dirname "$proj"); if [ -f "$PROJDIR/Program.cs" ]; then STARTUP="$proj"; break; fi; done; fi; \
      if [ -z "$STARTUP" ]; then STARTUP=$(echo "$PROJS" | head -n1); fi; \
    fi; \
    if [ -z "$STARTUP" ]; then echo "✗ No startup project found"; exit 1; fi; \
    dotnet watch --project "$STARTUP" run {{args}}

# Clean build artifacts (bin/ and obj/)
clean:
    @ROOT=$(skate get "{{_key-root}}@{{_db}}" 2>/dev/null); \
    if [ -z "$ROOT" ]; then DIR="$PWD"; while [ "$DIR" != "/" ]; do if ls "$DIR"/*.sln "$DIR"/*.slnx 2>/dev/null | head -n1 >/dev/null 2>&1; then ROOT="$DIR"; break; fi; DIR=$(dirname "$DIR"); done; if [ -z "$ROOT" ]; then DIR="$PWD"; while [ "$DIR" != "/" ]; do if ls "$DIR"/*.csproj 2>/dev/null | head -n1 >/dev/null 2>&1; then ROOT="$DIR"; break; fi; DIR=$(dirname "$DIR"); done; fi; fi; \
    if [ -n "$ROOT" ]; then find "$ROOT" -type d \( -name "bin" -o -name "obj" \) 2>/dev/null | xargs rm -rf; echo "✓ Cleaned bin/ and obj/ directories"; else echo "✗ Could not find project root"; fi

# List EF Core migrations and their status
migrations:
    @ROOT=$(skate get "{{_key-root}}@{{_db}}" 2>/dev/null); \
    if [ -z "$ROOT" ]; then DIR="$PWD"; while [ "$DIR" != "/" ]; do if ls "$DIR"/*.sln "$DIR"/*.slnx 2>/dev/null | head -n1 >/dev/null 2>&1; then ROOT="$DIR"; break; fi; DIR=$(dirname "$DIR"); done; if [ -z "$ROOT" ]; then DIR="$PWD"; while [ "$DIR" != "/" ]; do if ls "$DIR"/*.csproj 2>/dev/null | head -n1 >/dev/null 2>&1; then ROOT="$DIR"; break; fi; DIR=$(dirname "$DIR"); done; fi; fi; \
    RKEY=$(echo "$ROOT" | sed 's|/|__|g'); \
    DATA=$(skate get "${RKEY}-data@{{_db}}" 2>/dev/null); \
    STARTUP=$(skate get "${RKEY}-startup@{{_db}}" 2>/dev/null); \
    if [ -z "$DATA" ]; then PROJS=$(find "$ROOT" -name "*.csproj" | sort); for proj in $PROJS; do if grep -q "Microsoft.EntityFrameworkCore" "$proj" 2>/dev/null; then DATA="$proj"; break; fi; done; fi; \
    if [ -z "$STARTUP" ]; then PROJS=$(find "$ROOT" -name "*.csproj" | sort); for proj in $PROJS; do if grep -q '<OutputType>Exe</OutputType>' "$proj" 2>/dev/null || grep -q '<OutputType>WinExe</OutputType>' "$proj" 2>/dev/null; then STARTUP="$proj"; break; fi; done; if [ -z "$STARTUP" ]; then for proj in $PROJS; do PROJDIR=$(dirname "$proj"); if [ -f "$PROJDIR/Program.cs" ]; then STARTUP="$proj"; break; fi; done; fi; if [ -z "$STARTUP" ]; then STARTUP=$(echo "$PROJS" | head -n1); fi; fi; \
    if [ -z "$DATA" ]; then echo "✗ No data project found. Run 'just setup' to configure."; exit 1; fi; \
    if [ -z "$STARTUP" ]; then echo "✗ No startup project found. Run 'just setup' to configure."; exit 1; fi; \
    dotnet ef migrations list --project "$DATA" --startup-project "$STARTUP"

# Add a new EF Core migration (usage: just migrate AddUserTable or just migration-add AddUserTable)
migrate name:
    @ROOT=$(skate get "{{_key-root}}@{{_db}}" 2>/dev/null); \
    if [ -z "$ROOT" ]; then DIR="$PWD"; while [ "$DIR" != "/" ]; do if ls "$DIR"/*.sln "$DIR"/*.slnx 2>/dev/null | head -n1 >/dev/null 2>&1; then ROOT="$DIR"; break; fi; DIR=$(dirname "$DIR"); done; if [ -z "$ROOT" ]; then DIR="$PWD"; while [ "$DIR" != "/" ]; do if ls "$DIR"/*.csproj 2>/dev/null | head -n1 >/dev/null 2>&1; then ROOT="$DIR"; break; fi; DIR=$(dirname "$DIR"); done; fi; fi; \
    RKEY=$(echo "$ROOT" | sed 's|/|__|g'); \
    DATA=$(skate get "${RKEY}-data@{{_db}}" 2>/dev/null); \
    STARTUP=$(skate get "${RKEY}-startup@{{_db}}" 2>/dev/null); \
    if [ -z "$DATA" ]; then PROJS=$(find "$ROOT" -name "*.csproj" | sort); for proj in $PROJS; do if grep -q "Microsoft.EntityFrameworkCore" "$proj" 2>/dev/null; then DATA="$proj"; break; fi; done; fi; \
    if [ -z "$STARTUP" ]; then PROJS=$(find "$ROOT" -name "*.csproj" | sort); for proj in $PROJS; do if grep -q '<OutputType>Exe</OutputType>' "$proj" 2>/dev/null || grep -q '<OutputType>WinExe</OutputType>' "$proj" 2>/dev/null; then STARTUP="$proj"; break; fi; done; if [ -z "$STARTUP" ]; then for proj in $PROJS; do PROJDIR=$(dirname "$proj"); if [ -f "$PROJDIR/Program.cs" ]; then STARTUP="$proj"; break; fi; done; fi; if [ -z "$STARTUP" ]; then STARTUP=$(echo "$PROJS" | head -n1); fi; fi; \
    if [ -z "$DATA" ]; then echo "✗ No data project found. Run 'just setup' to configure."; exit 1; fi; \
    if [ -z "$STARTUP" ]; then echo "✗ No startup project found. Run 'just setup' to configure."; exit 1; fi; \
    dotnet ef migrations add {{name}} --project "$DATA" --startup-project "$STARTUP"; \
    STACK=$(skate get "${RKEY}-mig-stack@{{_db}}" 2>/dev/null || echo ""); \
    if [ -z "$STACK" ]; then NEW_STACK="{{name}}"; else NEW_STACK=$(printf '%s\n%s' "$STACK" "{{name}}"); fi; \
    skate set "${RKEY}-mig-stack@{{_db}}" "$NEW_STACK"; \
    echo "  ✓ '{{name}}' pushed onto migration stack"

# Alias for migrate command
migration-add name:
    @just migrate {{name}}

# Apply all pending EF Core migrations
update:
    @ROOT=$(skate get "{{_key-root}}@{{_db}}" 2>/dev/null); \
    if [ -z "$ROOT" ]; then DIR="$PWD"; while [ "$DIR" != "/" ]; do if ls "$DIR"/*.sln "$DIR"/*.slnx 2>/dev/null | head -n1 >/dev/null 2>&1; then ROOT="$DIR"; break; fi; DIR=$(dirname "$DIR"); done; if [ -z "$ROOT" ]; then DIR="$PWD"; while [ "$DIR" != "/" ]; do if ls "$DIR"/*.csproj 2>/dev/null | head -n1 >/dev/null 2>&1; then ROOT="$DIR"; break; fi; DIR=$(dirname "$DIR"); done; fi; fi; \
    RKEY=$(echo "$ROOT" | sed 's|/|__|g'); \
    DATA=$(skate get "${RKEY}-data@{{_db}}" 2>/dev/null); \
    STARTUP=$(skate get "${RKEY}-startup@{{_db}}" 2>/dev/null); \
    if [ -z "$DATA" ]; then PROJS=$(find "$ROOT" -name "*.csproj" | sort); for proj in $PROJS; do if grep -q "Microsoft.EntityFrameworkCore" "$proj" 2>/dev/null; then DATA="$proj"; break; fi; done; fi; \
    if [ -z "$STARTUP" ]; then PROJS=$(find "$ROOT" -name "*.csproj" | sort); for proj in $PROJS; do if grep -q '<OutputType>Exe</OutputType>' "$proj" 2>/dev/null || grep -q '<OutputType>WinExe</OutputType>' "$proj" 2>/dev/null; then STARTUP="$proj"; break; fi; done; if [ -z "$STARTUP" ]; then for proj in $PROJS; do PROJDIR=$(dirname "$proj"); if [ -f "$PROJDIR/Program.cs" ]; then STARTUP="$proj"; break; fi; done; fi; if [ -z "$STARTUP" ]; then STARTUP=$(echo "$PROJS" | head -n1); fi; fi; \
    if [ -z "$DATA" ]; then echo "✗ No data project found. Run 'just setup' to configure."; exit 1; fi; \
    if [ -z "$STARTUP" ]; then echo "✗ No startup project found. Run 'just setup' to configure."; exit 1; fi; \
    dotnet ef database update --project "$DATA" --startup-project "$STARTUP"

# Alias for update command
db-update:
    @just update

# Undo the last migration YOU added (uses migration stack)
rollback:
    @ROOT=$(skate get "{{_key-root}}@{{_db}}" 2>/dev/null); \
    if [ -z "$ROOT" ]; then DIR="$PWD"; while [ "$DIR" != "/" ]; do if ls "$DIR"/*.sln "$DIR"/*.slnx 2>/dev/null | head -n1 >/dev/null 2>&1; then ROOT="$DIR"; break; fi; DIR=$(dirname "$DIR"); done; if [ -z "$ROOT" ]; then DIR="$PWD"; while [ "$DIR" != "/" ]; do if ls "$DIR"/*.csproj 2>/dev/null | head -n1 >/dev/null 2>&1; then ROOT="$DIR"; break; fi; DIR=$(dirname "$DIR"); done; fi; fi; \
    RKEY=$(echo "$ROOT" | sed 's|/|__|g'); \
    DATA=$(skate get "${RKEY}-data@{{_db}}" 2>/dev/null); \
    STARTUP=$(skate get "${RKEY}-startup@{{_db}}" 2>/dev/null); \
    if [ -z "$DATA" ]; then PROJS=$(find "$ROOT" -name "*.csproj" | sort); for proj in $PROJS; do if grep -q "Microsoft.EntityFrameworkCore" "$proj" 2>/dev/null; then DATA="$proj"; break; fi; done; fi; \
    if [ -z "$STARTUP" ]; then PROJS=$(find "$ROOT" -name "*.csproj" | sort); for proj in $PROJS; do if grep -q '<OutputType>Exe</OutputType>' "$proj" 2>/dev/null || grep -q '<OutputType>WinExe</OutputType>' "$proj" 2>/dev/null; then STARTUP="$proj"; break; fi; done; if [ -z "$STARTUP" ]; then for proj in $PROJS; do PROJDIR=$(dirname "$proj"); if [ -f "$PROJDIR/Program.cs" ]; then STARTUP="$proj"; break; fi; done; fi; if [ -z "$STARTUP" ]; then STARTUP=$(echo "$PROJS" | head -n1); fi; fi; \
    if [ -z "$DATA" ]; then echo "✗ No data project found. Run 'just setup' to configure."; exit 1; fi; \
    if [ -z "$STARTUP" ]; then echo "✗ No startup project found. Run 'just setup' to configure."; exit 1; fi; \
    STACK=$(skate get "${RKEY}-mig-stack@{{_db}}" 2>/dev/null) || { echo "✗ Migration stack is empty — nothing to rollback."; exit 1; }; \
    LAST=$(printf '%s' "$STACK" | tail -n 1); \
    if [ -z "$LAST" ]; then echo "✗ Migration stack is empty — nothing to rollback."; exit 1; fi; \
    echo "  → Rolling back: $LAST"; \
    PREV=$(dotnet ef migrations list --project "$DATA" --startup-project "$STARTUP" 2>/dev/null | grep -v "^Build" | grep -v "^$" | sed -n "/${LAST}/!p" | tail -n 1 | awk '{print $1}'); \
    if [ -n "$PREV" ]; then echo "  → Reverting DB to: $PREV"; dotnet ef database update "$PREV" --project "$DATA" --startup-project "$STARTUP"; else echo "  → No previous migration — reverting to empty DB"; dotnet ef database update 0 --project "$DATA" --startup-project "$STARTUP"; fi; \
    dotnet ef migrations remove --project "$DATA" --startup-project "$STARTUP"; \
    NEW_STACK=$(printf '%s' "$STACK" | sed '$d'); \
    if [ -z "$NEW_STACK" ]; then skate delete "${RKEY}-mig-stack@{{_db}}" 2>/dev/null || true; else skate set "${RKEY}-mig-stack@{{_db}}" "$NEW_STACK"; fi; \
    echo "  ✓ Rollback complete."

# Alias for rollback command
migration-remove:
    @just rollback

# Fix migration conflicts by regenerating pending migrations
resolve name="MergeResolution":
    @ROOT=$(skate get "{{_key-root}}@{{_db}}" 2>/dev/null); \
    if [ -z "$ROOT" ]; then DIR="$PWD"; while [ "$DIR" != "/" ]; do if ls "$DIR"/*.sln "$DIR"/*.slnx 2>/dev/null | head -n1 >/dev/null 2>&1; then ROOT="$DIR"; break; fi; DIR=$(dirname "$DIR"); done; if [ -z "$ROOT" ]; then DIR="$PWD"; while [ "$DIR" != "/" ]; do if ls "$DIR"/*.csproj 2>/dev/null | head -n1 >/dev/null 2>&1; then ROOT="$DIR"; break; fi; DIR=$(dirname "$DIR"); done; fi; fi; \
    RKEY=$(echo "$ROOT" | sed 's|/|__|g'); \
    DATA=$(skate get "${RKEY}-data@{{_db}}" 2>/dev/null); \
    STARTUP=$(skate get "${RKEY}-startup@{{_db}}" 2>/dev/null); \
    if [ -z "$DATA" ]; then PROJS=$(find "$ROOT" -name "*.csproj" | sort); for proj in $PROJS; do if grep -q "Microsoft.EntityFrameworkCore" "$proj" 2>/dev/null; then DATA="$proj"; break; fi; done; fi; \
    if [ -z "$STARTUP" ]; then PROJS=$(find "$ROOT" -name "*.csproj" | sort); for proj in $PROJS; do if grep -q '<OutputType>Exe</OutputType>' "$proj" 2>/dev/null || grep -q '<OutputType>WinExe</OutputType>' "$proj" 2>/dev/null; then STARTUP="$proj"; break; fi; done; if [ -z "$STARTUP" ]; then for proj in $PROJS; do PROJDIR=$(dirname "$proj"); if [ -f "$PROJDIR/Program.cs" ]; then STARTUP="$proj"; break; fi; done; fi; if [ -z "$STARTUP" ]; then STARTUP=$(echo "$PROJS" | head -n1); fi; fi; \
    if [ -z "$DATA" ]; then echo "✗ No data project found. Run 'just setup' to configure."; exit 1; fi; \
    if [ -z "$STARTUP" ]; then echo "✗ No startup project found. Run 'just setup' to configure."; exit 1; fi; \
    echo "═══ Migration conflict resolver"; \
    FULL_LIST=$(dotnet ef migrations list --project "$DATA" --startup-project "$STARTUP" 2>/dev/null | grep -v "^Build" | grep -v "^$"); \
    if [ -z "$FULL_LIST" ]; then echo "✗ No migrations found."; exit 1; fi; \
    PENDING=$(echo "$FULL_LIST" | grep "(pending)" | awk '{print $1}'); \
    APPLIED=$(echo "$FULL_LIST" | grep -v "(pending)" | awk '{print $1}'); \
    if [ -z "$PENDING" ]; then echo "✗ No pending migrations detected. If you see git conflict markers in migration files, fix them manually first."; exit 1; fi; \
    echo "Applied:"; echo "$APPLIED" | sed 's/^/  /'; echo ""; \
    echo "Pending (will be regenerated):"; echo "$PENDING" | sed 's/^/  /'; echo ""; \
    SAFE=$(echo "$APPLIED" | tail -n 1); [ -z "$SAFE" ] && SAFE="0"; \
    echo "→ Reverting DB to: $SAFE …"; dotnet ef database update "$SAFE" --project "$DATA" --startup-project "$STARTUP"; \
    PENDING_COUNT=$(echo "$PENDING" | wc -l | tr -d ' '); \
    echo "→ Removing $PENDING_COUNT pending migration(s) …"; \
    for i in $(seq 1 "$PENDING_COUNT"); do dotnet ef migrations remove --project "$DATA" --startup-project "$STARTUP"; done; \
    echo "→ Regenerating as '{{name}}' …"; dotnet ef migrations add "{{name}}" --project "$DATA" --startup-project "$STARTUP"; \
    echo "→ Applying …"; dotnet ef database update --project "$DATA" --startup-project "$STARTUP"; \
    echo ""; echo "═══ Conflict resolved. Migration '{{name}}' is now applied."

# Add .csproj files to the solution (multi-select with fzf)
sln-add:
    @ROOT=$(skate get "{{_key-root}}@{{_db}}" 2>/dev/null); \
    if [ -z "$ROOT" ]; then DIR="$PWD"; while [ "$DIR" != "/" ]; do if ls "$DIR"/*.sln "$DIR"/*.slnx 2>/dev/null | head -n1 >/dev/null 2>&1; then ROOT="$DIR"; break; fi; DIR=$(dirname "$DIR"); done; if [ -z "$ROOT" ]; then DIR="$PWD"; while [ "$DIR" != "/" ]; do if ls "$DIR"/*.csproj 2>/dev/null | head -n1 >/dev/null 2>&1; then ROOT="$DIR"; break; fi; DIR=$(dirname "$DIR"); done; fi; fi; \
    SLN=$(ls "$ROOT"/*.slnx "$ROOT"/*.sln 2>/dev/null | head -n1); \
    if [ -z "$SLN" ]; then echo "✗ No .sln or .slnx found. Create one with: dotnet new sln"; exit 1; fi; \
    echo "Solution: $SLN"; echo ""; \
    PROJECTS=$(find "$ROOT" -name "*.csproj" | sort | fzf --prompt="Add to solution> " --multi); \
    if [ -z "$PROJECTS" ]; then echo "⊘ Nothing selected."; exit 0; fi; \
    echo ""; echo "$PROJECTS" | while read -r proj; do dotnet sln "$SLN" add "$proj"; done

# Add project-to-project reference(s)
ref:
    @ROOT=$(skate get "{{_key-root}}@{{_db}}" 2>/dev/null); \
    if [ -z "$ROOT" ]; then DIR="$PWD"; while [ "$DIR" != "/" ]; do if ls "$DIR"/*.sln "$DIR"/*.slnx 2>/dev/null | head -n1 >/dev/null 2>&1; then ROOT="$DIR"; break; fi; DIR=$(dirname "$DIR"); done; if [ -z "$ROOT" ]; then DIR="$PWD"; while [ "$DIR" != "/" ]; do if ls "$DIR"/*.csproj 2>/dev/null | head -n1 >/dev/null 2>&1; then ROOT="$DIR"; break; fi; DIR=$(dirname "$DIR"); done; fi; fi; \
    PROJECTS=$(find "$ROOT" -name "*.csproj" | sort); \
    if [ -z "$PROJECTS" ]; then echo "✗ No .csproj files found."; exit 1; fi; \
    CONSUMER=$(echo "$PROJECTS" | fzf --prompt="Consumer project> " --no-multi) || { echo "✗ Aborted."; exit 0; }; \
    echo "Consumer: $CONSUMER"; echo ""; \
    TARGETS=$(echo "$PROJECTS" | grep -v "^${CONSUMER}$" | fzf --prompt="References to add> " --multi) || { echo "✗ Aborted."; exit 0; }; \
    if [ -z "$TARGETS" ]; then echo "⊘ Nothing selected."; exit 0; fi; \
    echo ""; \
    TARGETS_INLINE=$(echo "$TARGETS" | tr '\n' ' '); \
    dotnet add "$CONSUMER" reference $TARGETS_INLINE

# Search NuGet and install a package
pkg term="":
    @ROOT=$(skate get "{{_key-root}}@{{_db}}" 2>/dev/null); \
    if [ -z "$ROOT" ]; then DIR="$PWD"; while [ "$DIR" != "/" ]; do if ls "$DIR"/*.sln "$DIR"/*.slnx 2>/dev/null | head -n1 >/dev/null 2>&1; then ROOT="$DIR"; break; fi; DIR=$(dirname "$DIR"); done; if [ -z "$ROOT" ]; then DIR="$PWD"; while [ "$DIR" != "/" ]; do if ls "$DIR"/*.csproj 2>/dev/null | head -n1 >/dev/null 2>&1; then ROOT="$DIR"; break; fi; DIR=$(dirname "$DIR"); done; fi; fi; \
    if [ -z "{{term}}" ]; then echo -n "Search NuGet for: "; read -r TERM; [ -z "$TERM" ] && { echo "✗ No search term."; exit 1; }; else TERM="{{term}}"; fi; \
    echo "Searching NuGet for '$TERM' …"; \
    RESULTS=$(dotnet package search "$TERM" --take 30 --format json 2>/dev/null | jq -r '.searchResult[].packages[] | "\(.id) \(.latestVersion)"'); \
    if [ -z "$RESULTS" ]; then echo "✗ No packages found for '$TERM'."; exit 1; fi; \
    echo ""; \
    PICKED=$(echo "$RESULTS" | fzf --prompt="Package> " --no-multi) || { echo "✗ Aborted."; exit 0; }; \
    PKG_ID=$(echo "$PICKED" | awk '{print $1}'); \
    echo "Selected: $PKG_ID"; echo ""; \
    TARGET=$(find "$ROOT" -name "*.csproj" | sort | fzf --prompt="Install into> " --no-multi) || { echo "✗ Aborted."; exit 0; }; \
    echo "Target: $TARGET"; echo ""; \
    dotnet add "$TARGET" package "$PKG_ID"

# EF Core tools wrapper (install/update/uninstall/status)
fx cmd="status":
    @case "{{cmd}}" in \
      install) echo "→ Installing dotnet-ef global tool…"; dotnet tool install --global dotnet-ef ;; \
      update) echo "→ Updating dotnet-ef global tool…"; dotnet tool update --global dotnet-ef ;; \
      uninstall) echo "→ Uninstalling dotnet-ef global tool…"; dotnet tool uninstall --global dotnet-ef ;; \
      status|*) if dotnet ef --version 2>/dev/null; then echo ""; echo "✓ dotnet-ef is installed"; else echo "✗ dotnet-ef not found. Install with: just fx install"; fi ;; \
    esac

# Create a new .NET project from templates
new *args:
    @dotnet new {{args}}

# Run dotnet tool commands
tool *args:
    @dotnet tool {{args}}

# Format code with dotnet format
format *args:
    @ROOT=$(skate get "{{_key-root}}@{{_db}}" 2>/dev/null); \
    if [ -z "$ROOT" ]; then DIR="$PWD"; while [ "$DIR" != "/" ]; do if ls "$DIR"/*.sln "$DIR"/*.slnx 2>/dev/null | head -n1 >/dev/null 2>&1; then ROOT="$DIR"; break; fi; DIR=$(dirname "$DIR"); done; if [ -z "$ROOT" ]; then DIR="$PWD"; while [ "$DIR" != "/" ]; do if ls "$DIR"/*.csproj 2>/dev/null | head -n1 >/dev/null 2>&1; then ROOT="$DIR"; break; fi; DIR=$(dirname "$DIR"); done; fi; fi; \
    SLN=$(ls "$ROOT"/*.slnx "$ROOT"/*.sln 2>/dev/null | head -n1); \
    if [ -n "$SLN" ]; then dotnet format "$SLN" {{args}}; else echo "✗ No solution file found."; exit 1; fi

# Check code formatting without making changes (lint)
lint *args:
    @ROOT=$(skate get "{{_key-root}}@{{_db}}" 2>/dev/null); \
    if [ -z "$ROOT" ]; then DIR="$PWD"; while [ "$DIR" != "/" ]; do if ls "$DIR"/*.sln "$DIR"/*.slnx 2>/dev/null | head -n1 >/dev/null 2>&1; then ROOT="$DIR"; break; fi; DIR=$(dirname "$DIR"); done; if [ -z "$ROOT" ]; then DIR="$PWD"; while [ "$DIR" != "/" ]; do if ls "$DIR"/*.csproj 2>/dev/null | head -n1 >/dev/null 2>&1; then ROOT="$DIR"; break; fi; DIR=$(dirname "$DIR"); done; fi; fi; \
    SLN=$(ls "$ROOT"/*.slnx "$ROOT"/*.sln 2>/dev/null | head -n1); \
    if [ -n "$SLN" ]; then dotnet format "$SLN" --verify-no-changes {{args}}; else echo "✗ No solution file found."; exit 1; fi

# Publish Telegram bot to namespaced output directory
publish-bot:
    @echo "→ Publishing Telegram bot to publish/bot/"; \
    dotnet publish TimeSheet.Presentation.Telegram/TimeSheet.Presentation.Telegram.csproj \
      -c Release \
      -o publish/bot/ \
      --os linux --arch x64

# Publish REST API to namespaced output directory
publish-api:
    @echo "→ Publishing REST API to publish/api/"; \
    dotnet publish TimeSheet.Presentation.API/TimeSheet.Presentation.API.csproj \
      -c Release \
      -o publish/api/ \
      --os linux --arch x64

# Build frontend for production to namespaced output directory
build-frontend:
    @echo "→ Building frontend to publish/frontend/"; \
    cd TimeSheet.Frontend && npm run build; \
    mkdir -p ../publish/frontend; \
    cp -r build/* ../publish/frontend/

# Run API with hot reload (dotnet watch)
dev-api:
    @echo "→ Starting API in watch mode…"; \
    dotnet watch --project TimeSheet.Presentation.API/TimeSheet.Presentation.API.csproj run

# Run SvelteKit dev server
dev-frontend:
    @echo "→ Starting SvelteKit dev server…"; \
    cd TimeSheet.Frontend && npm run dev

# Run both API and frontend in dev mode (requires terminal multiplexer or separate terminals)
dev:
    @echo "Run 'just dev-api' in one terminal and 'just dev-frontend' in another"

# Check status of all 3 services
status:
    @RKEY=$(echo "$PWD" | sed 's|/|__|g'); \
    BOT_PID=$(skate get "${RKEY}-bot-pid@{{_db}}" 2>/dev/null); \
    API_PID=$(skate get "${RKEY}-api-pid@{{_db}}" 2>/dev/null); \
    FRONTEND_PID=$(skate get "${RKEY}-frontend-pid@{{_db}}" 2>/dev/null); \
    echo "═══ Service Status"; \
    if [ -n "$BOT_PID" ] && ps -p "$BOT_PID" > /dev/null 2>&1; then \
      echo "  ✓ Telegram bot: RUNNING (PID $BOT_PID)"; \
    else \
      echo "  ✗ Telegram bot: STOPPED"; \
      [ -n "$BOT_PID" ] && skate delete "${RKEY}-bot-pid@{{_db}}" 2>/dev/null || true; \
    fi; \
    if [ -n "$API_PID" ] && ps -p "$API_PID" > /dev/null 2>&1; then \
      echo "  ✓ API:          RUNNING (PID $API_PID)"; \
    else \
      echo "  ✗ API:          STOPPED"; \
      [ -n "$API_PID" ] && skate delete "${RKEY}-api-pid@{{_db}}" 2>/dev/null || true; \
    fi; \
    if [ -n "$FRONTEND_PID" ] && ps -p "$FRONTEND_PID" > /dev/null 2>&1; then \
      echo "  ✓ Frontend:     RUNNING (PID $FRONTEND_PID)"; \
    else \
      echo "  ✗ Frontend:     STOPPED"; \
      [ -n "$FRONTEND_PID" ] && skate delete "${RKEY}-frontend-pid@{{_db}}" 2>/dev/null || true; \
    fi; \
    true

# Toggle all 3 services (bot, API, frontend) on/off
toggle:
    @RKEY=$(echo "$PWD" | sed 's|/|__|g'); \
    BOT_PID=$(skate get "${RKEY}-bot-pid@{{_db}}" 2>/dev/null); \
    API_PID=$(skate get "${RKEY}-api-pid@{{_db}}" 2>/dev/null); \
    FRONTEND_PID=$(skate get "${RKEY}-frontend-pid@{{_db}}" 2>/dev/null); \
    if [ -n "$BOT_PID" ] || [ -n "$API_PID" ] || [ -n "$FRONTEND_PID" ]; then \
      echo "═══ Stopping services…"; \
      if [ -n "$BOT_PID" ]; then \
        if ps -p "$BOT_PID" > /dev/null 2>&1; then \
          pkill -P "$BOT_PID" 2>/dev/null || true; \
          kill "$BOT_PID" 2>/dev/null && echo "  ✓ Telegram bot stopped (PID $BOT_PID)" || echo "  ⊘ Telegram bot already stopped"; \
        else \
          echo "  ⊘ Telegram bot not running"; \
        fi; \
        skate delete "${RKEY}-bot-pid@{{_db}}" 2>/dev/null || true; \
      fi; \
      if [ -n "$API_PID" ]; then \
        if ps -p "$API_PID" > /dev/null 2>&1; then \
          pkill -P "$API_PID" 2>/dev/null || true; \
          kill "$API_PID" 2>/dev/null && echo "  ✓ API stopped (PID $API_PID)" || echo "  ⊘ API already stopped"; \
        else \
          echo "  ⊘ API not running"; \
        fi; \
        skate delete "${RKEY}-api-pid@{{_db}}" 2>/dev/null || true; \
      fi; \
      if [ -n "$FRONTEND_PID" ]; then \
        if ps -p "$FRONTEND_PID" > /dev/null 2>&1; then \
          pkill -TERM -P "$FRONTEND_PID" 2>/dev/null || true; \
          sleep 0.5; \
          pkill -9 -P "$FRONTEND_PID" 2>/dev/null || true; \
          kill -TERM "$FRONTEND_PID" 2>/dev/null || true; \
          sleep 0.5; \
          kill -9 "$FRONTEND_PID" 2>/dev/null || true; \
          echo "  ✓ Frontend stopped (PID $FRONTEND_PID)"; \
        else \
          echo "  ⊘ Frontend not running"; \
        fi; \
        skate delete "${RKEY}-frontend-pid@{{_db}}" 2>/dev/null || true; \
      fi; \
      pkill -9 -f "vite.*TimeSheet.Frontend" 2>/dev/null || true; \
      pkill -9 -f "esbuild.*TimeSheet" 2>/dev/null || true; \
      pkill -9 -f "node.*vite" 2>/dev/null || true; \
      echo ""; \
      echo "All services stopped."; \
    else \
      echo "═══ Starting services…"; \
      dotnet run --project TimeSheet.Presentation.Telegram/TimeSheet.Presentation.Telegram.csproj > /tmp/timesheet-bot.log 2>&1 & \
      BOT_PID=$!; \
      skate set "${RKEY}-bot-pid@{{_db}}" "$BOT_PID"; \
      echo "  ✓ Telegram bot started (PID $BOT_PID)"; \
      dotnet run --project TimeSheet.Presentation.API/TimeSheet.Presentation.API.csproj > /tmp/timesheet-api.log 2>&1 & \
      API_PID=$!; \
      skate set "${RKEY}-api-pid@{{_db}}" "$API_PID"; \
      echo "  ✓ API started (PID $API_PID)"; \
      cd TimeSheet.Frontend && npm run dev > /tmp/timesheet-frontend.log 2>&1 & \
      FRONTEND_PID=$!; \
      cd ..; \
      skate set "${RKEY}-frontend-pid@{{_db}}" "$FRONTEND_PID"; \
      echo "  ✓ Frontend started (PID $FRONTEND_PID)"; \
      echo ""; \
      echo "All services started. Use 'just toggle' again to stop them."; \
      echo ""; \
      echo "Logs:"; \
      echo "  - Bot:      tail -f /tmp/timesheet-bot.log"; \
      echo "  - API:      tail -f /tmp/timesheet-api.log"; \
      echo "  - Frontend: tail -f /tmp/timesheet-frontend.log"; \
    fi

# Start Docker Compose services
docker-up:
    @docker compose up -d

# Stop Docker Compose services
docker-down:
    @docker compose down

# Build and push container image to ghcr.io
publish-container:
    dotnet publish TimeSheet.Presentation.Telegram \
      -c Release \
      --os linux --arch x64 \
      /t:PublishContainer \
      /p:ContainerRepository=catalinplesu/timesheet \
      /p:ContainerImageTag=latest \
      /p:ContainerRegistry=ghcr.io

# Restore NuGet packages
restore:
    @ROOT=$(skate get "{{_key-root}}@{{_db}}" 2>/dev/null); \
    if [ -z "$ROOT" ]; then DIR="$PWD"; while [ "$DIR" != "/" ]; do if ls "$DIR"/*.sln "$DIR"/*.slnx 2>/dev/null | head -n1 >/dev/null 2>&1; then ROOT="$DIR"; break; fi; DIR=$(dirname "$DIR"); done; if [ -z "$ROOT" ]; then DIR="$PWD"; while [ "$DIR" != "/" ]; do if ls "$DIR"/*.csproj 2>/dev/null | head -n1 >/dev/null 2>&1; then ROOT="$DIR"; break; fi; DIR=$(dirname "$DIR"); done; fi; fi; \
    SLN=$(ls "$ROOT"/*.slnx "$ROOT"/*.sln 2>/dev/null | head -n1); \
    if [ -n "$SLN" ]; then dotnet restore "$SLN"; else dotnet restore; fi
