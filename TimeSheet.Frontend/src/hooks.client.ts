import { auth } from '$lib/stores/auth';
import { scheduleTokenRefresh } from '$lib/utils/tokenRefresh';
import { get } from 'svelte/store';

// Initialize token refresh on app start
if (typeof window !== 'undefined') {
	const authState = get(auth);
	if (authState.isAuthenticated && authState.refreshToken) {
		scheduleTokenRefresh();
	}
}
