import { auth } from '$lib/stores/auth';
import { apiClient, RefreshTokenRequest } from '$lib/api';
import { get } from 'svelte/store';

let refreshTimer: ReturnType<typeof setTimeout> | null = null;

/**
 * Schedule token refresh before expiry
 * Refreshes 5 minutes before token expires
 */
export function scheduleTokenRefresh() {
	// Clear any existing timer
	if (refreshTimer) {
		clearTimeout(refreshTimer);
		refreshTimer = null;
	}

	const authState = get(auth);
	if (!authState.isAuthenticated || !authState.expiresAt || !authState.refreshToken) {
		return;
	}

	const now = new Date();
	const expiresAt = new Date(authState.expiresAt);
	const msUntilExpiry = expiresAt.getTime() - now.getTime();

	// Refresh 5 minutes before expiry (or immediately if less than 5 minutes remain)
	const refreshBuffer = 5 * 60 * 1000; // 5 minutes in milliseconds
	const msUntilRefresh = Math.max(0, msUntilExpiry - refreshBuffer);

	refreshTimer = setTimeout(async () => {
		await refreshToken();
	}, msUntilRefresh);
}

/**
 * Manually refresh the access token
 */
export async function refreshToken(): Promise<boolean> {
	const authState = get(auth);
	if (!authState.refreshToken) {
		console.error('No refresh token available');
		auth.logout();
		return false;
	}

	try {
		const request = new RefreshTokenRequest({ refreshToken: authState.refreshToken });
		const response = await apiClient.refresh(request);

		auth.updateToken(response.accessToken, response.refreshToken, response.expiresAt, response.utcOffsetMinutes);
		scheduleTokenRefresh();
		return true;
	} catch (error) {
		console.error('Token refresh failed:', error);
		auth.logout();
		return false;
	}
}

/**
 * Cancel any scheduled token refresh
 */
export function cancelTokenRefresh() {
	if (refreshTimer) {
		clearTimeout(refreshTimer);
		refreshTimer = null;
	}
}
