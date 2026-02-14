import { writable } from 'svelte/store';

interface AuthState {
	token: string | null;
	isAuthenticated: boolean;
}

const TOKEN_KEY = 'timesheet_auth_token';

function createAuthStore() {
	// Initialize from localStorage if available
	const storedToken = typeof window !== 'undefined' ? localStorage.getItem(TOKEN_KEY) : null;

	const { subscribe, set, update } = writable<AuthState>({
		token: storedToken,
		isAuthenticated: !!storedToken
	});

	return {
		subscribe,
		login: (token: string) => {
			set({ token, isAuthenticated: true });
			if (typeof window !== 'undefined') {
				localStorage.setItem(TOKEN_KEY, token);
			}
		},
		logout: () => {
			set({ token: null, isAuthenticated: false });
			if (typeof window !== 'undefined') {
				localStorage.removeItem(TOKEN_KEY);
			}
		}
	};
}

export const auth = createAuthStore();
