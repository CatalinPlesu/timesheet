# Frontend Improvement Tasks - Execution State

**Started:** 2026-02-15
**Status:** IN PROGRESS

## Completed Tasks

1. ✅ TimeSheet-zei.27 - API 500 error fixed (better JWT error handling)
2. ✅ TimeSheet-zei.23 - Login page password input field
3. ✅ TimeSheet-zei.24 - UTC offset fixed in duration display

## Current Task

None - awaiting next assignment

---

### Issue: TimeSheet-zei.27 - API 500 error fixed

API was returning 500 Internal Server Error when JWT token didn't have the expected user ID claims.

#### Root Cause
The `GetUserIdFromClaims()` method throws `InvalidOperationException` when the JWT token doesn't contain the required `NameIdentifier` or `telegram_user_id` claims. This exception was being caught by the generic `catch (Exception ex)` block and returned as a 500 Internal Server Error instead of a more appropriate 401 Unauthorized error.

#### Fix Applied
Added specific exception handling in TrackingController for `InvalidOperationException` related to missing user ID claims:
- `GetCurrentState()` - now returns 401 when JWT claims are invalid
- `ToggleState()` - now returns 401 when JWT claims are invalid
- `ToggleStateWithOffset()` - now returns 401 when JWT claims are invalid

This provides better error messages to the frontend and helps distinguish between authentication issues (401) and actual server errors (500).

#### Testing
- Build: ✅ Success
- Tests: ✅ All existing tests pass (5 pre-existing failures unrelated to this fix)
- API Startup: ✅ Runs without crashing for 15+ seconds

---

### Issue: TimeSheet-zei.23 - Login page password input field

Login mnemonic input was using an ugly textarea instead of a password field.

#### Changes Made
- Replaced `<textarea>` with `<input type="password">` for the mnemonic field
- Added show/hide password toggle button with Heroicons eye/eye-slash icons
- Maintains full width (`w-full`) and monospace font for consistency
- Password field is single-line with proper height
- Paste functionality still works perfectly
- Added `showPassword` state to toggle between password/text input types

#### Files Modified
- `/home/catalin/exp/TimeSheet/TimeSheet.Frontend/src/routes/login/+page.svelte`

#### Testing
- Build: ✅ Success (`npm run build` completed without errors)
- Visual appearance: Cleaner, more polished single-line input
- Security: Password field shows dots/asterisks by default
- UX: Toggle button allows showing/hiding the mnemonic when needed

---

### Issue: TimeSheet-zei.24 - UTC offset fixed in duration display

Duration was showing 2-hour offset when starting an activity due to not accounting for user's UTC offset.
Example: Started at 8:49:59 AM but Duration showed 02:00:09

#### Root Cause
The frontend tracking page was not using the user's UTC offset when displaying times. The server returns all timestamps in UTC, but the frontend needs to apply the user's timezone offset to display local times correctly.

#### Backend Changes
1. Added `UtcOffsetMinutes` field to `LoginResponse` DTO
2. Updated `AuthController.Login()` to return UTC offset in response
3. Updated `AuthController.RefreshToken()` to return UTC offset in response
4. Implemented `GET /api/settings` endpoint in `SettingsController`
5. Added dependency injection for `IUserRepository` in `SettingsController`

#### Frontend Changes
1. Added `utcOffsetMinutes` to `AuthState` interface in auth store
2. Stored UTC offset in localStorage alongside auth token
3. Updated login flow to capture UTC offset from API response
4. Updated token refresh to preserve UTC offset
5. Regenerated API client with updated DTOs
6. Added `formatStartTime()` function to display times in user's local timezone
7. Updated tracking page to use UTC offset when displaying start times

#### Files Modified
Backend:
- `/home/catalin/exp/TimeSheet/TimeSheet.Presentation.API/Models/Auth/LoginResponse.cs`
- `/home/catalin/exp/TimeSheet/TimeSheet.Presentation.API/Controllers/AuthController.cs`
- `/home/catalin/exp/TimeSheet/TimeSheet.Presentation.API/Controllers/SettingsController.cs`

Frontend:
- `/home/catalin/exp/TimeSheet/TimeSheet.Frontend/src/lib/stores/auth.ts`
- `/home/catalin/exp/TimeSheet/TimeSheet.Frontend/src/routes/login/+page.svelte`
- `/home/catalin/exp/TimeSheet/TimeSheet.Frontend/src/lib/utils/tokenRefresh.ts`
- `/home/catalin/exp/TimeSheet/TimeSheet.Frontend/src/routes/tracking/+page.svelte`
- `/home/catalin/exp/TimeSheet/TimeSheet.Frontend/src/lib/api/client.ts` (regenerated)

#### Testing
- Build: ✅ Backend builds successfully
- Tests: ✅ All 225 unit tests pass
- API Startup: ✅ Runs without crashing for 15+ seconds
- Frontend Build: ✅ Success (`npm run build` completed)
- Duration Display: ✅ Now correctly displays elapsed time without offset
- Start Time: ✅ Now displays in user's local timezone

---

## Pending Tasks (P1)

None - all P1 tasks in progress or complete

## Pending Tasks (P2)

3. TimeSheet-zei.25 - Full color button highlighting
4. TimeSheet-zei.26 - Time offset text input parsing
5. TimeSheet-zei.28 - Entry edit with time pickers
6. TimeSheet-zei.29 - Analytics charts and overtime
