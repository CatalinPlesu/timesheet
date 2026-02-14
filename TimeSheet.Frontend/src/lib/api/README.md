# TimeSheet API Client

This directory contains the auto-generated TypeScript client for the TimeSheet API, along with authentication helpers.

## Files

- `client.ts` - Auto-generated NSwag client (DO NOT EDIT MANUALLY)
- `index.ts` - Authenticated API client wrapper with JWT token injection
- `test-client.ts` - Example usage demonstrating how to use the client

## Generating the Client

The client is auto-generated from the API's OpenAPI specification. To regenerate:

1. Ensure the API is running at `http://localhost:5191`
2. Run the generation script:

```bash
npm run generate-client
```

This will fetch the latest OpenAPI spec and regenerate `client.ts`.

## Configuration

The NSwag configuration is in `nswag.json` at the project root. Key settings:

- **Runtime**: Net100 (matches our .NET version)
- **Source**: API OpenAPI spec at `http://localhost:5191/openapi/v1.json`
- **Output**: `src/lib/api/client.ts`
- **Template**: Fetch API (browser-native)
- **TypeScript**: 5.9

## Usage

### Basic Usage

Import the pre-configured client with JWT authentication:

```typescript
import { apiClient } from '$lib/api';

// The client automatically injects JWT tokens from the auth store
const averages = await apiClient.dailyAverages();
```

### Authentication

Login and store the JWT token:

```typescript
import { apiClient, LoginRequest } from '$lib/api';
import { auth } from '$lib/stores/auth';

// Login with mnemonic
const request = new LoginRequest({
  mnemonic: 'word1 word2 word3 ... word24'
});

const response = await apiClient.login(request);

// Store the token (persists to localStorage)
auth.login(response.token);

// All subsequent API calls will include the token
```

### Available Endpoints

#### Analytics

```typescript
// Daily averages
const averages = await apiClient.dailyAverages(startDate, endDate);

// Commute patterns
const patterns = await apiClient.commutePatterns('ToWork', startDate, endDate);

// Period aggregates
const aggregate = await apiClient.periodAggregate(startDate, endDate);

// Daily breakdown
const breakdown = await apiClient.dailyBreakdown(startDate, endDate);

// Chart data
const chartData = await apiClient.chartData(startDate, endDate, 'day');
```

#### Tracking

```typescript
import { TrackingStateRequest } from '$lib/api';

// Get current state
const current = await apiClient.current();

// Toggle state (0=Idle, 1=Commuting, 2=Working, 3=Lunch)
const request = new TrackingStateRequest({ state: 2 }); // Working
const response = await apiClient.toggle(request);

// Toggle with offset
import { TrackingStateWithOffsetRequest } from '$lib/api';
const offsetRequest = new TrackingStateWithOffsetRequest({
  state: 2,
  offsetMinutes: -5 // Started 5 minutes ago
});
const offsetResponse = await apiClient.toggleWithOffset(offsetRequest);
```

#### Entries

```typescript
// List entries
const entries = await apiClient.entriesGET(startDate, endDate, groupBy, page, pageSize);

// Get single entry
const entry = await apiClient.entriesGET2(entryId);

// Update entry
import { EntryUpdateRequest } from '$lib/api';
const updateRequest = new EntryUpdateRequest({
  startTime: new Date(),
  endTime: new Date()
});
const updated = await apiClient.entriesPUT(entryId, updateRequest);

// Delete entry
await apiClient.entriesDELETE(entryId);
```

#### Settings

```typescript
import {
  UpdateUtcOffsetRequest,
  UpdateAutoShutdownRequest,
  UpdateLunchReminderRequest,
  UpdateTargetHoursRequest,
  UpdateForgotThresholdRequest
} from '$lib/api';

// Get current settings
const settings = await apiClient.settings();

// Update UTC offset
const utcRequest = new UpdateUtcOffsetRequest({ offsetMinutes: -300 }); // EST
await apiClient.utcOffset(utcRequest);

// Update auto-shutdown
const autoShutdownRequest = new UpdateAutoShutdownRequest({
  enabled: true,
  thresholdMinutes: 600
});
await apiClient.autoShutdown(autoShutdownRequest);
```

## Authentication Flow

The client uses a custom fetch wrapper that automatically injects JWT tokens:

1. User logs in with mnemonic
2. API returns JWT token
3. Token is stored in auth store (and localStorage)
4. All subsequent requests automatically include `Authorization: Bearer <token>` header
5. If token expires, user must re-authenticate

## Error Handling

The client throws typed exceptions for API errors:

```typescript
import { ApiException } from '$lib/api';

try {
  await apiClient.dailyAverages();
} catch (error) {
  if (error instanceof ApiException) {
    console.error(`API Error ${error.status}: ${error.message}`);
  }
}
```

## Environment Configuration

The API base URL can be configured via environment variable:

```bash
# .env
VITE_API_URL=https://api.example.com/
```

Default: `http://localhost:5191/`

## TypeScript Support

All API models are fully typed. Import types as needed:

```typescript
import type {
  DailyAveragesDto,
  TrackingStateResponse,
  UserSettingsDto,
  EntryListResponse
} from '$lib/api';
```

## Regeneration Notes

When regenerating the client after API changes:

1. Update API endpoints/models
2. Run API locally
3. Run `npm run generate-client`
4. Commit the updated `client.ts`
5. Manual fix may be needed for void return types (see commit history)
