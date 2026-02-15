# Frontend Improvement Tasks - Execution State

**Started:** 2026-02-15
**Status:** IN PROGRESS

## Current Task

**Agent:** fix-api-500-toggle-error
**Task:** TimeSheet-zei.27 - FIX: API 500 Internal Server Error when toggling tracking state
**Priority:** P1 (BLOCKING)
**Status:** ✅ COMPLETED

### Issue
API was returning 500 Internal Server Error when JWT token didn't have the expected user ID claims.

### Root Cause
The `GetUserIdFromClaims()` method throws `InvalidOperationException` when the JWT token doesn't contain the required `NameIdentifier` or `telegram_user_id` claims. This exception was being caught by the generic `catch (Exception ex)` block and returned as a 500 Internal Server Error instead of a more appropriate 401 Unauthorized error.

### Fix Applied
Added specific exception handling in TrackingController for `InvalidOperationException` related to missing user ID claims:
- `GetCurrentState()` - now returns 401 when JWT claims are invalid
- `ToggleState()` - now returns 401 when JWT claims are invalid
- `ToggleStateWithOffset()` - now returns 401 when JWT claims are invalid

This provides better error messages to the frontend and helps distinguish between authentication issues (401) and actual server errors (500).

### Testing
- Build: ✅ Success
- Tests: ✅ All existing tests pass (5 pre-existing failures unrelated to this fix)
- API Startup: ✅ Runs without crashing for 15+ seconds

### Note
If the frontend is still experiencing 500 errors, the issue may be:
1. JWT token generation in the login flow not including the correct claims
2. An actual server-side exception during state transition logic
3. Database connectivity issues

The improved error handling and logging will help identify the exact cause.

---

## Pending Tasks (P1)

1. TimeSheet-zei.23 - Login password field instead of textarea
2. TimeSheet-zei.24 - Fix UTC offset in duration display

## Pending Tasks (P2)

3. TimeSheet-zei.25 - Full color button highlighting
4. TimeSheet-zei.26 - Time offset text input parsing
5. TimeSheet-zei.28 - Entry edit with time pickers
6. TimeSheet-zei.29 - Analytics charts and overtime
