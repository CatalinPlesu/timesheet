import { writable } from 'svelte/store';

interface AuthState {
	token: string | null;
	isAuthenticated: boolean;
}

function createAuthStore() {
	const { subscribe, set, update } = writable<AuthState>({
		token: null,
		isAuthenticated: false
	});

	return {
		subscribe,
		login: (token: string) => {
			set({ token, isAuthenticated: true });
			// TODO: Store token in localStorage
		},
		logout: () => {
			set({ token: null, isAuthenticated: false });
			// TODO: Clear token from localStorage
		}
	};
}

export const auth = createAuthStore();
