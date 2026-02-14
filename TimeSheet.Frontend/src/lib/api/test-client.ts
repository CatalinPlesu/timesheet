/**
 * Test file to verify the API client imports correctly
 * This file is not meant to be run, just to verify TypeScript compilation
 */

import {
	apiClient,
	LoginRequest,
	type LoginResponse,
	TrackingStateRequest,
	type TrackingStateResponse
} from './index';

// Test that we can use the client
export async function testLogin(mnemonic: string): Promise<LoginResponse> {
	const request = new LoginRequest({
		mnemonic
	});

	return await apiClient.login(request);
}

// Test that we can use analytics endpoints
export async function testDailyAverages() {
	return await apiClient.dailyAverages();
}

// Test that we can use tracking endpoints
export async function testToggleCommute(): Promise<TrackingStateResponse> {
	const request = new TrackingStateRequest({
		state: 1 // Commuting
	});

	return await apiClient.toggle(request);
}

// Test that we can use entries endpoints
export async function testGetEntries() {
	return await apiClient.entriesGET();
}
