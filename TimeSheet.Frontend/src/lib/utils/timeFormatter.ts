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

/**
 * Converts a UTC timestamp to local time using the user's UTC offset.
 * @param utcTime UTC timestamp (Date object or ISO string)
 * @param utcOffsetMinutes User's UTC offset in minutes (from auth store)
 * @returns Date object in local timezone
 */
export function utcToLocal(utcTime: Date | string, utcOffsetMinutes: number): Date {
	const date = new Date(utcTime);
	// Apply offset to UTC time
	return new Date(date.getTime() + utcOffsetMinutes * 60 * 1000);
}

/**
 * Formats a timestamp in local time for display.
 * @param utcTime UTC timestamp (Date object or ISO string)
 * @param utcOffsetMinutes User's UTC offset in minutes (from auth store)
 * @returns Formatted string like "Jan 15, 2026, 14:30"
 */
export function formatLocalDateTime(utcTime: Date | string | null | undefined, utcOffsetMinutes: number): string {
	if (!utcTime) return 'N/A';

	const localTime = utcToLocal(utcTime, utcOffsetMinutes);

	// Use UTC methods on the adjusted time to avoid timezone conversion
	return localTime.toLocaleString('en-US', {
		year: 'numeric',
		month: 'short',
		day: 'numeric',
		hour: '2-digit',
		minute: '2-digit',
		timeZone: 'UTC'
	});
}

/**
 * Formats a timestamp in local time as HH:MM:SS.
 * @param utcTime UTC timestamp (Date object or ISO string)
 * @param utcOffsetMinutes User's UTC offset in minutes (from auth store)
 * @returns Formatted string like "14:30:45"
 */
export function formatLocalTime(utcTime: Date | string | null | undefined, utcOffsetMinutes: number): string {
	if (!utcTime) return 'N/A';

	const localTime = utcToLocal(utcTime, utcOffsetMinutes);

	// Format as HH:MM:SS using UTC methods (since we already applied the offset)
	const hours = localTime.getUTCHours().toString().padStart(2, '0');
	const minutes = localTime.getUTCMinutes().toString().padStart(2, '0');
	const seconds = localTime.getUTCSeconds().toString().padStart(2, '0');

	return `${hours}:${minutes}:${seconds}`;
}
