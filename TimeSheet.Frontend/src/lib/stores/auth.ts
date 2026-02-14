import { writable } from 'svelte/store';

interface AuthState {
	token: string | null;
	refreshToken: string | null;
	expiresAt: Date | null;
	isAuthenticated: boolean;
}

const TOKEN_KEY = 'timesheet_auth_token';
const REFRESH_TOKEN_KEY = 'timesheet_refresh_token';
const EXPIRES_AT_KEY = 'timesheet_expires_at';

function createAuthStore() {
	// Initialize from localStorage if available
	const storedToken = typeof window !== 'undefined' ? localStorage.getItem(TOKEN_KEY) : null;
	const storedRefreshToken = typeof window !== 'undefined' ? localStorage.getItem(REFRESH_TOKEN_KEY) : null;
	const storedExpiresAt = typeof window !== 'undefined' ? localStorage.getItem(EXPIRES_AT_KEY) : null;

	const { subscribe, set, update } = writable<AuthState>({
		token: storedToken,
		refreshToken: storedRefreshToken,
		expiresAt: storedExpiresAt ? new Date(storedExpiresAt) : null,
		isAuthenticated: !!storedToken
	});

	return {
		subscribe,
		login: (token: string, refreshToken: string | null | undefined, expiresAt: Date) => {
			const state = {
				token,
				refreshToken: refreshToken ?? null,
				expiresAt,
				isAuthenticated: true
			};
			set(state);
			if (typeof window !== 'undefined') {
				localStorage.setItem(TOKEN_KEY, token);
				if (refreshToken) {
					localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
				}
				localStorage.setItem(EXPIRES_AT_KEY, expiresAt.toISOString());
			}
		},
		logout: () => {
			set({ token: null, refreshToken: null, expiresAt: null, isAuthenticated: false });
			if (typeof window !== 'undefined') {
				localStorage.removeItem(TOKEN_KEY);
				localStorage.removeItem(REFRESH_TOKEN_KEY);
				localStorage.removeItem(EXPIRES_AT_KEY);
			}
		},
		updateToken: (token: string, refreshToken: string | null | undefined, expiresAt: Date) => {
			update(state => ({
				...state,
				token,
				refreshToken: refreshToken ?? state.refreshToken,
				expiresAt
			}));
			if (typeof window !== 'undefined') {
				localStorage.setItem(TOKEN_KEY, token);
				if (refreshToken) {
					localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
				}
				localStorage.setItem(EXPIRES_AT_KEY, expiresAt.toISOString());
			}
		}
	};
}

export const auth = createAuthStore();
