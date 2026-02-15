import { ApiException, ProblemDetails } from '$lib/api';

/**
 * Extracts a user-friendly error message from an API error.
 * Prioritizes ProblemDetails fields: detail > title > default message.
 */
export function extractErrorMessage(error: unknown, defaultMessage = 'An error occurred'): string {
	// Check if it's an ApiException with ProblemDetails
	if (ApiException.isApiException(error) && error.result) {
		const problemDetails = error.result as ProblemDetails;

		// Prefer detail over title (detail is more specific)
		if (problemDetails.detail) {
			return problemDetails.detail;
		}

		if (problemDetails.title) {
			return problemDetails.title;
		}
	}

	// Fallback to error message if available
	if (error instanceof Error && error.message) {
		return error.message;
	}

	// Last resort: default message
	return defaultMessage;
}
