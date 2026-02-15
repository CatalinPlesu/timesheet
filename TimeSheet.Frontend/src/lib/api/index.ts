import { Client } from './client';
import { get } from 'svelte/store';
import { auth } from '$lib/stores/auth';

/**
 * Custom fetch wrapper that adds JWT token to requests
 */
class AuthenticatedFetch {
	async fetch(url: RequestInfo, init?: RequestInit): Promise<Response> {
		const authState = get(auth);
		const headers = new Headers(init?.headers);

		// Add Authorization header if token exists
		if (authState.token) {
			headers.set('Authorization', `Bearer ${authState.token}`);
		}

		// Merge headers back into init
		const authenticatedInit: RequestInit = {
			...init,
			headers
		};

		return fetch(url, authenticatedInit);
	}
}

/**
 * Get the API base URL from environment or default to localhost
 */
function getBaseUrl(): string {
	// In production, this would come from environment variables
	// For now, default to localhost
	return import.meta.env.VITE_API_URL || 'http://localhost:5191';
}

/**
 * Singleton instance of the API client with JWT authentication
 */
export const apiClient = new Client(getBaseUrl(), new AuthenticatedFetch());

/**
 * Export all types from the generated client for convenience
 */
export * from './client';
