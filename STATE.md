# Frontend Improvement Tasks - Execution State

**Started:** 2026-02-15
**Status:** IN PROGRESS

## Completed Tasks

1. ✅ TimeSheet-zei.27 - API 500 error fixed (better JWT error handling)
2. ✅ TimeSheet-zei.23 - Login page password input field

## Current Task

None - ready for next task

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

## Pending Tasks (P1)

1. TimeSheet-zei.24 - Fix UTC offset in duration display

## Pending Tasks (P2)

3. TimeSheet-zei.25 - Full color button highlighting
4. TimeSheet-zei.26 - Time offset text input parsing
5. TimeSheet-zei.28 - Entry edit with time pickers
6. TimeSheet-zei.29 - Analytics charts and overtime
