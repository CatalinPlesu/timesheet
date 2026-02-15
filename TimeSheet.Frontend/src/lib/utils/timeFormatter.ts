/**
 * Provides consistent time formatting utilities across the frontend.
 * All durations should be displayed as hours:minutes (HH:MM or "Xh Ym"), never as decimal hours.
 */

/**
 * Formats a duration in hours as "Xh Ym" (e.g., "8h 30m" or "45m").
 * @param hours Duration in decimal hours (e.g., 8.5)
 * @returns Formatted string like "8h 30m" or "45m"
 */
export function formatDuration(hours: number | null | undefined): string {
	if (hours === null || hours === undefined) {
		return 'N/A';
	}

	const absHours = Math.abs(hours);
	let h = Math.floor(absHours);
	let m = Math.round((absHours - h) * 60);

	// Handle rounding edge case where 59.5 minutes rounds to 60
	if (m >= 60) {
		h++;
		m -= 60;
	}

	if (h >= 1) {
		return m > 0 ? `${h}h ${m}m` : `${h}h`;
	}

	return `${m}m`;
}

/**
 * Formats a duration as HH:MM (e.g., "08:30" or "00:45").
 * @param hours Duration in decimal hours
 * @returns Formatted string like "08:30"
 */
export function formatDurationAsTime(hours: number | null | undefined): string {
	if (hours === null || hours === undefined) {
		return '00:00';
	}

	const absHours = Math.abs(hours);
	let h = Math.floor(absHours);
	let m = Math.round((absHours - h) * 60);

	// Handle rounding edge case
	if (m >= 60) {
		h++;
		m -= 60;
	}

	return `${h.toString().padStart(2, '0')}:${m.toString().padStart(2, '0')}`;
}
