/**
 * Decode a JWT token without verification.
 * This is safe for extracting claims from a token that has already been validated by the server.
 */
export function decodeJwt(token: string): Record<string, any> | null {
	try {
		// JWT has 3 parts separated by dots: header.payload.signature
		const parts = token.split('.');
		if (parts.length !== 3) {
			return null;
		}

		// Decode the payload (second part)
		const payload = parts[1];
		// Replace URL-safe base64 characters
		const base64 = payload.replace(/-/g, '+').replace(/_/g, '/');
		// Add padding if needed
		const padded = base64.padEnd(base64.length + (4 - (base64.length % 4)) % 4, '=');
		// Decode base64
		const decoded = atob(padded);
		// Parse JSON
		return JSON.parse(decoded);
	} catch (error) {
		console.error('Failed to decode JWT:', error);
		return null;
	}
}

/**
 * Extract the Telegram user ID from a JWT token
 */
export function getTelegramUserIdFromToken(token: string): string | null {
	const payload = decodeJwt(token);
	if (!payload) {
		return null;
	}

	// Try multiple claim names
	return payload.telegram_user_id || payload.sub || payload.nameid || null;
}

/**
 * Extract the username from a JWT token
 */
export function getUsernameFromToken(token: string): string | null {
	const payload = decodeJwt(token);
	if (!payload) {
		return null;
	}

	return payload.telegram_username || payload.name || null;
}

/**
 * Check if the user is an admin from a JWT token
 */
export function isAdminFromToken(token: string): boolean {
	const payload = decodeJwt(token);
	if (!payload) {
		return false;
	}

	const isAdmin = payload.is_admin;
	return isAdmin === 'true' || isAdmin === true;
}
