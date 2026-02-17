<script lang="ts">
	import { onMount } from 'svelte';
	import { apiClient, type TrackingEntryDto, type EntryListResponse, EntryUpdateRequest } from '$lib/api';
	import { extractErrorMessage } from '$lib/utils/errorHandling';
	import { formatDuration, formatLocalDateTime, utcToLocal } from '$lib/utils/timeFormatter';
	import { auth } from '$lib/stores/auth';

	// Icons (Heroicons - outline)
	const PencilSquareIcon = `<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-5 h-5">
		<path stroke-linecap="round" stroke-linejoin="round" d="m16.862 4.487 1.687-1.688a1.875 1.875 0 1 1 2.652 2.652L10.582 16.07a4.5 4.5 0 0 1-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 0 1 1.13-1.897l8.932-8.931Zm0 0L19.5 7.125M18 14v4.75A2.25 2.25 0 0 1 15.75 21H5.25A2.25 2.25 0 0 1 3 18.75V8.25A2.25 2.25 0 0 1 5.25 6H10" />
	</svg>`;

	const TrashIcon = `<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-5 h-5">
		<path stroke-linecap="round" stroke-linejoin="round" d="m14.74 9-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 0 1-2.244 2.077H8.084a2.25 2.25 0 0 1-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 0 0-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 0 1 3.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 0 0-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 0 0-7.5 0" />
	</svg>`;

	const ChevronLeftIcon = `<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-5 h-5">
		<path stroke-linecap="round" stroke-linejoin="round" d="M15.75 19.5 8.25 12l7.5-7.5" />
	</svg>`;

	const ChevronRightIcon = `<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-5 h-5">
		<path stroke-linecap="round" stroke-linejoin="round" d="m8.25 4.5 7.5 7.5-7.5 7.5" />
	</svg>`;

	// State
	let entries: TrackingEntryDto[] = $state([]);
	let totalCount = $state(0);
	let page = $state(1);
	let pageSize = $state(50);
	let totalPages = $state(0);
	let groupBy = $state(1); // 1=Day, 2=Week, 3=Month, 4=Year
	let loading = $state(false);
	let error = $state<string | null>(null);

	// Date range filters (computed from currentPeriod)
	let startDate = $state<Date | undefined>(undefined);
	let endDate = $state<Date | undefined>(undefined);

	// Current period navigation
	let currentPeriod = $state<Date>(new Date()); // Defaults to today

	// Edit modal state
	let editModalOpen = $state(false);
	let editEntry = $state<TrackingEntryDto | null>(null);
	let adjustmentMinutes = $state(0);
	let editError = $state<string | null>(null);
	let editLoading = $state(false);
	let editStartTime = $state('');
	let editEndTime = $state('');

	// Delete modal state
	let deleteModalOpen = $state(false);
	let deleteEntry = $state<TrackingEntryDto | null>(null);
	let deleteError = $state<string | null>(null);
	let deleteLoading = $state(false);

	// Calculate date range based on groupBy and currentPeriod
	function calculateDateRange() {
		if (groupBy === 0) {
			// No grouping - show all entries
			startDate = undefined;
			endDate = undefined;
			return;
		}

		const utcOffsetMinutes = $auth.utcOffsetMinutes ?? 0;

		if (groupBy === 1) {
			// Day grouping - show single day
			const localDate = utcToLocal(currentPeriod, utcOffsetMinutes);
			const dayStart = new Date(Date.UTC(
				localDate.getUTCFullYear(),
				localDate.getUTCMonth(),
				localDate.getUTCDate(),
				0, 0, 0, 0
			));
			const dayEnd = new Date(Date.UTC(
				localDate.getUTCFullYear(),
				localDate.getUTCMonth(),
				localDate.getUTCDate(),
				23, 59, 59, 999
			));
			// Convert back to UTC
			startDate = new Date(dayStart.getTime() - utcOffsetMinutes * 60 * 1000);
			endDate = new Date(dayEnd.getTime() - utcOffsetMinutes * 60 * 1000);
		} else if (groupBy === 2) {
			// Week grouping - show week (Monday to Sunday)
			const localDate = utcToLocal(currentPeriod, utcOffsetMinutes);
			const dayOfWeek = localDate.getUTCDay(); // 0=Sunday, 1=Monday, etc.
			const daysToMonday = dayOfWeek === 0 ? 6 : dayOfWeek - 1; // Adjust to make Monday = 0

			const weekStart = new Date(Date.UTC(
				localDate.getUTCFullYear(),
				localDate.getUTCMonth(),
				localDate.getUTCDate() - daysToMonday,
				0, 0, 0, 0
			));
			const weekEnd = new Date(Date.UTC(
				localDate.getUTCFullYear(),
				localDate.getUTCMonth(),
				localDate.getUTCDate() - daysToMonday + 6,
				23, 59, 59, 999
			));
			// Convert back to UTC
			startDate = new Date(weekStart.getTime() - utcOffsetMinutes * 60 * 1000);
			endDate = new Date(weekEnd.getTime() - utcOffsetMinutes * 60 * 1000);
		} else if (groupBy === 3) {
			// Month grouping - show entire month
			const localDate = utcToLocal(currentPeriod, utcOffsetMinutes);
			const monthStart = new Date(Date.UTC(
				localDate.getUTCFullYear(),
				localDate.getUTCMonth(),
				1,
				0, 0, 0, 0
			));
			const monthEnd = new Date(Date.UTC(
				localDate.getUTCFullYear(),
				localDate.getUTCMonth() + 1,
				0, // Last day of month
				23, 59, 59, 999
			));
			// Convert back to UTC
			startDate = new Date(monthStart.getTime() - utcOffsetMinutes * 60 * 1000);
			endDate = new Date(monthEnd.getTime() - utcOffsetMinutes * 60 * 1000);
		} else if (groupBy === 4) {
			// Year grouping - show entire year
			const localDate = utcToLocal(currentPeriod, utcOffsetMinutes);
			const yearStart = new Date(Date.UTC(
				localDate.getUTCFullYear(),
				0, 1,
				0, 0, 0, 0
			));
			const yearEnd = new Date(Date.UTC(
				localDate.getUTCFullYear(),
				11, 31,
				23, 59, 59, 999
			));
			// Convert back to UTC
			startDate = new Date(yearStart.getTime() - utcOffsetMinutes * 60 * 1000);
			endDate = new Date(yearEnd.getTime() - utcOffsetMinutes * 60 * 1000);
		}
	}

	// Fetch entries from API
	async function fetchEntries() {
		calculateDateRange();
		loading = true;
		error = null;
		try {
			const response = await apiClient.entriesGET(startDate, endDate, groupBy, page, pageSize);
			entries = response.entries;
			totalCount = response.totalCount;
			totalPages = response.totalPages;
		} catch (err: any) {
			error = extractErrorMessage(err, 'Failed to load entries');
			console.error('Failed to fetch entries:', err);
		} finally {
			loading = false;
		}
	}

	// Format date for display (in user's local timezone)
	function formatDate(date: Date | null | undefined): string {
		const utcOffsetMinutes = $auth.utcOffsetMinutes ?? 0;
		return formatLocalDateTime(date, utcOffsetMinutes);
	}

	// Format duration for display (wrapper to handle 'Active' state)
	function formatDurationDisplay(hours: number | null | undefined): string {
		if (hours === null || hours === undefined) return 'Active';
		return formatDuration(hours);
	}

	// Get state name
	function getStateName(state: number): string {
		switch (state) {
			case 0: return 'None';
			case 1: return 'Commute';
			case 2: return 'Work';
			case 3: return 'Lunch';
			default: return 'Unknown';
		}
	}

	// Get state badge color
	function getStateBadge(state: number): string {
		switch (state) {
			case 1: return 'badge-info';
			case 2: return 'badge-success';
			case 3: return 'badge-warning';
			default: return 'badge-ghost';
		}
	}

	// Navigate to previous period
	function navigatePrevious() {
		const utcOffsetMinutes = $auth.utcOffsetMinutes ?? 0;
		const localDate = utcToLocal(currentPeriod, utcOffsetMinutes);

		if (groupBy === 1) {
			// Previous day
			localDate.setUTCDate(localDate.getUTCDate() - 1);
		} else if (groupBy === 2) {
			// Previous week
			localDate.setUTCDate(localDate.getUTCDate() - 7);
		} else if (groupBy === 3) {
			// Previous month
			localDate.setUTCMonth(localDate.getUTCMonth() - 1);
		} else if (groupBy === 4) {
			// Previous year
			localDate.setUTCFullYear(localDate.getUTCFullYear() - 1);
		}

		// Convert back to UTC (auto-refresh will trigger via $effect)
		currentPeriod = new Date(localDate.getTime() - utcOffsetMinutes * 60 * 1000);
	}

	// Navigate to next period
	function navigateNext() {
		const utcOffsetMinutes = $auth.utcOffsetMinutes ?? 0;
		const localDate = utcToLocal(currentPeriod, utcOffsetMinutes);

		if (groupBy === 1) {
			// Next day
			localDate.setUTCDate(localDate.getUTCDate() + 1);
		} else if (groupBy === 2) {
			// Next week
			localDate.setUTCDate(localDate.getUTCDate() + 7);
		} else if (groupBy === 3) {
			// Next month
			localDate.setUTCMonth(localDate.getUTCMonth() + 1);
		} else if (groupBy === 4) {
			// Next year
			localDate.setUTCFullYear(localDate.getUTCFullYear() + 1);
		}

		// Convert back to UTC (auto-refresh will trigger via $effect)
		currentPeriod = new Date(localDate.getTime() - utcOffsetMinutes * 60 * 1000);
	}

	// Jump to today
	function jumpToToday() {
		// Auto-refresh will trigger via $effect
		currentPeriod = new Date();
	}

	// Get current period label for display
	function getCurrentPeriodLabel(): string {
		if (groupBy === 0) return 'All Entries';

		const utcOffsetMinutes = $auth.utcOffsetMinutes ?? 0;
		const localDate = utcToLocal(currentPeriod, utcOffsetMinutes);

		if (groupBy === 1) {
			// Day: "January 15, 2026"
			return localDate.toLocaleDateString('en-US', {
				timeZone: 'UTC',
				year: 'numeric',
				month: 'long',
				day: 'numeric'
			});
		} else if (groupBy === 2) {
			// Week: "Week of Jan 15 - Jan 21, 2026"
			const dayOfWeek = localDate.getUTCDay();
			const daysToMonday = dayOfWeek === 0 ? 6 : dayOfWeek - 1;

			const weekStart = new Date(Date.UTC(
				localDate.getUTCFullYear(),
				localDate.getUTCMonth(),
				localDate.getUTCDate() - daysToMonday
			));
			const weekEnd = new Date(Date.UTC(
				localDate.getUTCFullYear(),
				localDate.getUTCMonth(),
				localDate.getUTCDate() - daysToMonday + 6
			));

			const startStr = weekStart.toLocaleDateString('en-US', {
				timeZone: 'UTC',
				month: 'short',
				day: 'numeric'
			});
			const endStr = weekEnd.toLocaleDateString('en-US', {
				timeZone: 'UTC',
				month: 'short',
				day: 'numeric',
				year: 'numeric'
			});

			return `Week of ${startStr} - ${endStr}`;
		} else if (groupBy === 3) {
			// Month: "January 2026"
			return localDate.toLocaleDateString('en-US', {
				timeZone: 'UTC',
				year: 'numeric',
				month: 'long'
			});
		} else if (groupBy === 4) {
			// Year: "2026"
			return localDate.getUTCFullYear().toString();
		}

		return '';
	}

	// Format date for input (YYYY-MM-DD in local timezone)
	function formatDateForInput(date: Date): string {
		const utcOffsetMinutes = $auth.utcOffsetMinutes ?? 0;
		const localDate = utcToLocal(date, utcOffsetMinutes);
		const year = localDate.getUTCFullYear();
		const month = (localDate.getUTCMonth() + 1).toString().padStart(2, '0');
		const day = localDate.getUTCDate().toString().padStart(2, '0');
		return `${year}-${month}-${day}`;
	}

	// Handle date picker change
	function handleDatePickerChange(event: Event) {
		const input = event.target as HTMLInputElement;
		const dateStr = input.value; // YYYY-MM-DD format
		if (!dateStr) return;

		const [year, month, day] = dateStr.split('-').map(Number);
		const utcOffsetMinutes = $auth.utcOffsetMinutes ?? 0;

		// Create date in local timezone
		const localDate = new Date(Date.UTC(year, month - 1, day, 12, 0, 0, 0));

		// Convert to UTC (auto-refresh will trigger via $effect)
		currentPeriod = new Date(localDate.getTime() - utcOffsetMinutes * 60 * 1000);
	}

	// Convert Date to HH:MM format for time input (in user's local timezone)
	function dateToTimeString(date: Date | null | undefined): string {
		if (!date) return '';
		const utcOffsetMinutes = $auth.utcOffsetMinutes ?? 0;
		const localDate = utcToLocal(date, utcOffsetMinutes);
		const hours = localDate.getUTCHours().toString().padStart(2, '0');
		const minutes = localDate.getUTCMinutes().toString().padStart(2, '0');
		return `${hours}:${minutes}`;
	}

	// Convert time string (HH:MM) to Date by combining with a date (converts from local to UTC)
	function timeStringToDate(timeStr: string, baseDate: Date): Date {
		const [hours, minutes] = timeStr.split(':').map(Number);
		const utcOffsetMinutes = $auth.utcOffsetMinutes ?? 0;

		// Get the local date for the base date
		const localBase = utcToLocal(baseDate, utcOffsetMinutes);

		// Set the time in local timezone
		const localDate = new Date(Date.UTC(
			localBase.getUTCFullYear(),
			localBase.getUTCMonth(),
			localBase.getUTCDate(),
			hours,
			minutes,
			0,
			0
		));

		// Convert back to UTC
		return new Date(localDate.getTime() - utcOffsetMinutes * 60 * 1000);
	}

	// Calculate adjustment minutes from edited times
	function calculateAdjustment(): number {
		if (!editEntry || !editEndTime) return 0;

		const originalEnd = editEntry.endedAt ? new Date(editEntry.endedAt) : null;
		if (!originalEnd) return 0;

		const newEnd = timeStringToDate(editEndTime, originalEnd);
		const diffMs = newEnd.getTime() - originalEnd.getTime();
		return Math.round(diffMs / 60000); // Convert to minutes
	}

	// Open edit modal
	function openEditModal(entry: TrackingEntryDto) {
		editEntry = entry;
		adjustmentMinutes = 0;
		editError = null;
		editStartTime = dateToTimeString(entry.startedAt);
		editEndTime = dateToTimeString(entry.endedAt);
		editModalOpen = true;
	}

	// Close edit modal
	function closeEditModal() {
		editModalOpen = false;
		editEntry = null;
		adjustmentMinutes = 0;
		editError = null;
		editStartTime = '';
		editEndTime = '';
	}

	// Handle edit save
	async function saveEdit() {
		if (!editEntry) return;

		// Calculate adjustment from time pickers
		const adjustment = calculateAdjustment();

		if (adjustment === 0) {
			editError = 'Please change the end time to make an adjustment';
			return;
		}

		editLoading = true;
		editError = null;
		try {
			const request = new EntryUpdateRequest({
				adjustmentMinutes: adjustment
			});

			const updated = await apiClient.entriesPUT(editEntry.id, request);

			// Optimistic update
			const index = entries.findIndex(e => e.id === editEntry!.id);
			if (index !== -1) {
				entries[index] = updated;
			}

			closeEditModal();

			// Refresh to confirm
			await fetchEntries();
		} catch (err: any) {
			editError = extractErrorMessage(err, 'Failed to update entry');
			console.error('Failed to update entry:', err);
		} finally {
			editLoading = false;
		}
	}

	// Open delete modal
	function openDeleteModal(entry: TrackingEntryDto) {
		deleteEntry = entry;
		deleteError = null;
		deleteModalOpen = true;
	}

	// Close delete modal
	function closeDeleteModal() {
		deleteModalOpen = false;
		deleteEntry = null;
		deleteError = null;
	}

	// Handle delete confirm
	async function confirmDelete() {
		if (!deleteEntry) return;

		deleteLoading = true;
		deleteError = null;
		try {
			await apiClient.entriesDELETE(deleteEntry.id);

			// Optimistic update
			entries = entries.filter(e => e.id !== deleteEntry!.id);
			totalCount--;

			closeDeleteModal();

			// Refresh to confirm
			await fetchEntries();
		} catch (err: any) {
			deleteError = extractErrorMessage(err, 'Failed to delete entry');
			console.error('Failed to delete entry:', err);
		} finally {
			deleteLoading = false;
		}
	}

	// Calculate preview duration from time pickers
	function getPreviewDuration(): string {
		if (!editEntry || !editStartTime || !editEndTime) return '';

		const startDate = new Date(editEntry.startedAt);
		const start = timeStringToDate(editStartTime, startDate);

		const endDate = editEntry.endedAt ? new Date(editEntry.endedAt) : startDate;
		const end = timeStringToDate(editEndTime, endDate);

		// If end is before start, assume it's the next day
		if (end <= start) {
			end.setDate(end.getDate() + 1);
		}

		const durationMs = end.getTime() - start.getTime();
		const durationHours = durationMs / (1000 * 60 * 60);
		return formatDuration(durationHours);
	}

	// Handle keyboard events for modals
	function handleEscapeKey(event: KeyboardEvent) {
		if (event.key === 'Escape') {
			if (editModalOpen) closeEditModal();
			if (deleteModalOpen) closeDeleteModal();
		}
	}

	let initialLoadComplete = $state(false);

	// Load entries on mount
	onMount(() => {
		fetchEntries();
		// Mark initial load as complete after fetch
		setTimeout(() => {
			initialLoadComplete = true;
		}, 100);
	});

	// Auto-refresh when filters change
	$effect(() => {
		// Read reactive dependencies to track them
		const currentGroupBy = groupBy;
		const period = currentPeriod;

		// Skip the initial load (onMount handles that)
		if (initialLoadComplete) {
			fetchEntries();
		}
	});
</script>

<svelte:window onkeydown={handleEscapeKey} />

<div class="max-w-7xl mx-auto px-4">
	<div class="mb-8">
		<h1 class="text-4xl font-bold mb-2">Audit Table</h1>
		<p class="text-base-content/70">Review and manage your time tracking entries</p>
	</div>

	<!-- Filters and Controls -->
	<div class="card bg-base-200 shadow-xl mb-6">
		<div class="card-body p-6">
			<h2 class="card-title text-xl mb-4">
				<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-6 h-6">
					<path stroke-linecap="round" stroke-linejoin="round" d="M10.5 6h9.75M10.5 6a1.5 1.5 0 1 1-3 0m3 0a1.5 1.5 0 1 0-3 0M3.75 6H7.5m3 12h9.75m-9.75 0a1.5 1.5 0 0 1-3 0m3 0a1.5 1.5 0 0 0-3 0m-3.75 0H7.5m9-6h3.75m-3.75 0a1.5 1.5 0 0 1-3 0m3 0a1.5 1.5 0 0 0-3 0m-9.75 0h9.75" />
				</svg>
				Filters
			</h2>
			<div class="flex flex-col gap-4">
				<!-- Group By Select -->
				<div class="form-control w-full sm:w-56">
					<label class="label" for="groupBy">
						<span class="label-text font-semibold">Group By</span>
					</label>
					<select
						id="groupBy"
						class="select select-bordered w-full"
						bind:value={groupBy}
					>
						<option value={1}>Day</option>
						<option value={2}>Week</option>
						<option value={3}>Month</option>
						<option value={4}>Year</option>
					</select>
				</div>

				<!-- Period Navigation (only shown when groupBy is active) -->
				{#if groupBy > 0}
					<div class="divider my-2"></div>
					<div class="flex flex-col gap-4">
						<!-- Current Period Display -->
						<div class="flex items-center justify-between">
							<h3 class="text-lg font-semibold">
								{getCurrentPeriodLabel()}
							</h3>
							<button
								class="btn btn-sm btn-ghost"
								onclick={jumpToToday}
								disabled={loading}
								title="Jump to today"
							>
								Today
							</button>
						</div>

						<!-- Navigation Controls -->
						<div class="flex flex-col sm:flex-row gap-4 items-stretch sm:items-center">
							<!-- Previous/Next Buttons -->
							<div class="join w-full sm:w-auto">
								<button
									class="btn btn-outline join-item flex-1"
									onclick={navigatePrevious}
									disabled={loading}
									aria-label="Previous period"
								>
									{@html ChevronLeftIcon}
									<span class="hidden sm:inline">Previous</span>
								</button>
								<button
									class="btn btn-outline join-item flex-1"
									onclick={navigateNext}
									disabled={loading}
									aria-label="Next period"
								>
									<span class="hidden sm:inline">Next</span>
									{@html ChevronRightIcon}
								</button>
							</div>

							<!-- Date Picker -->
							<div class="form-control w-full sm:flex-1">
								<input
									type="date"
									class="input input-bordered w-full"
									value={formatDateForInput(currentPeriod)}
									onchange={handleDatePickerChange}
									disabled={loading}
									aria-label="Jump to date"
								/>
							</div>
						</div>
					</div>
				{/if}
			</div>
		</div>
	</div>

	<!-- Error Display -->
	{#if error}
		<div class="alert alert-error mb-6">
			<span>{error}</span>
		</div>
	{/if}

	<!-- Entries Table -->
	<div class="card bg-base-200 shadow-xl">
		<div class="card-body p-6">
			<h2 class="card-title text-xl mb-4">
				<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-6 h-6">
					<path stroke-linecap="round" stroke-linejoin="round" d="M3.375 19.5h17.25m-17.25 0a1.125 1.125 0 01-1.125-1.125M3.375 19.5h7.5c.621 0 1.125-.504 1.125-1.125m-9.75 0V5.625m0 12.75v-1.5c0-.621.504-1.125 1.125-1.125m18.375 2.625V5.625m0 12.75c0 .621-.504 1.125-1.125 1.125m1.125-1.125v-1.5c0-.621-.504-1.125-1.125-1.125m0 3.75h-7.5A1.125 1.125 0 0112 18.375m9.75-12.75c0-.621-.504-1.125-1.125-1.125H3.375c-.621 0-1.125.504-1.125 1.125m19.5 0v1.5c0 .621-.504 1.125-1.125 1.125M2.25 5.625v1.5c0 .621.504 1.125 1.125 1.125m0 0h17.25m-17.25 0h7.5c.621 0 1.125.504 1.125 1.125M3.375 8.25c-.621 0-1.125.504-1.125 1.125v1.5c0 .621.504 1.125 1.125 1.125m17.25-3.75h-7.5c-.621 0-1.125.504-1.125 1.125m8.625-1.125c.621 0 1.125.504 1.125 1.125v1.5c0 .621-.504 1.125-1.125 1.125m-17.25 0h7.5m-7.5 0c-.621 0-1.125.504-1.125 1.125v1.5c0 .621.504 1.125 1.125 1.125M12 10.875v-1.5m0 1.5c0 .621-.504 1.125-1.125 1.125M12 10.875c0 .621.504 1.125 1.125 1.125m-2.25 0c.621 0 1.125.504 1.125 1.125M13.125 12h7.5m-7.5 0c-.621 0-1.125.504-1.125 1.125M20.625 12c.621 0 1.125.504 1.125 1.125v1.5c0 .621-.504 1.125-1.125 1.125m-17.25 0h7.5M12 14.625v-1.5m0 1.5c0 .621-.504 1.125-1.125 1.125M12 14.625c0 .621.504 1.125 1.125 1.125m-2.25 0c.621 0 1.125.504 1.125 1.125m0 1.5v-1.5m0 0c0-.621.504-1.125 1.125-1.125m0 0h7.5" />
				</svg>
				Time Entries
			</h2>

			{#if loading}
				<div class="flex justify-center items-center py-12">
					<span class="loading loading-spinner loading-lg"></span>
				</div>
			{:else if entries.length === 0}
				<div class="text-center py-12 text-base-content/70">
					<p>No entries found</p>
				</div>
			{:else}
				<div class="overflow-x-auto -mx-6">
					<table class="table table-zebra">
						<thead>
							<tr>
								<th class="bg-base-300">Type</th>
								<th class="bg-base-300">Start Time</th>
								<th class="bg-base-300">End Time</th>
								<th class="bg-base-300">Duration</th>
								<th class="bg-base-300">Status</th>
								<th class="text-right bg-base-300">Actions</th>
							</tr>
						</thead>
						<tbody>
							{#each entries as entry (entry.id)}
								<tr>
									<td>
										<span class="badge {getStateBadge(entry.state)}">
											{getStateName(entry.state)}
										</span>
									</td>
									<td>{formatDate(entry.startedAt)}</td>
									<td>{formatDate(entry.endedAt)}</td>
									<td>{formatDurationDisplay(entry.durationHours)}</td>
									<td>
										{#if entry.isActive}
											<span class="badge badge-success">Active</span>
										{:else}
											<span class="badge badge-ghost">Completed</span>
										{/if}
									</td>
									<td class="text-right">
										<div class="flex justify-end gap-2">
											<button
												class="btn btn-sm btn-ghost"
												onclick={() => openEditModal(entry)}
												aria-label="Edit entry"
												title="Edit entry"
											>
												{@html PencilSquareIcon}
											</button>
											<button
												class="btn btn-sm btn-ghost text-error"
												onclick={() => openDeleteModal(entry)}
												aria-label="Delete entry"
												title="Delete entry"
											>
												{@html TrashIcon}
											</button>
										</div>
									</td>
								</tr>
							{/each}
						</tbody>
					</table>
				</div>

				<!-- Pagination Info -->
				<div class="flex flex-col sm:flex-row justify-between items-center gap-4 mt-6 pt-4 border-t border-base-300">
					<div class="text-sm text-base-content/70">
						Showing <span class="font-semibold">{entries.length}</span> of <span class="font-semibold">{totalCount}</span> entries
					</div>
					<div class="text-sm text-base-content/70">
						Page <span class="font-semibold">{page}</span> of <span class="font-semibold">{totalPages}</span>
					</div>
				</div>
			{/if}
		</div>
	</div>
</div>

<!-- Edit Modal -->
{#if editModalOpen && editEntry}
	<div class="modal modal-open" role="dialog" aria-labelledby="edit-modal-title" aria-modal="true">
		<div class="modal-box max-w-2xl">
			<h3 id="edit-modal-title" class="font-bold text-lg mb-4">Edit Entry</h3>

			<!-- Entry Details -->
			<div class="space-y-4">
				<div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
					<div>
						<div class="label">
							<span class="label-text">Type</span>
						</div>
						<span class="badge {getStateBadge(editEntry.state)} badge-lg">
							{getStateName(editEntry.state)}
						</span>
					</div>
					<div>
						<div class="label">
							<span class="label-text">Status</span>
						</div>
						<span class="badge {editEntry.isActive ? 'badge-success' : 'badge-ghost'} badge-lg">
							{editEntry.isActive ? 'Active' : 'Completed'}
						</span>
					</div>
				</div>

				<div class="divider">Original Times</div>

				<div class="grid grid-cols-1 sm:grid-cols-2 gap-4 bg-base-300 p-4 rounded-lg">
					<div>
						<div class="label">
							<span class="label-text">Start Time</span>
						</div>
						<div class="text-base-content">{formatDate(editEntry.startedAt)}</div>
					</div>
					<div>
						<div class="label">
							<span class="label-text">End Time</span>
						</div>
						<div class="text-base-content">{formatDate(editEntry.endedAt)}</div>
					</div>
					<div class="sm:col-span-2">
						<div class="label">
							<span class="label-text">Original Duration</span>
						</div>
						<div class="text-base-content">{formatDurationDisplay(editEntry.durationHours)}</div>
					</div>
				</div>

				<div class="divider">Edit Times</div>

				<!-- Time Pickers -->
				<div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
					<div class="form-control">
						<label class="label" for="edit-start-time">
							<span class="label-text font-semibold">Start Time</span>
						</label>
						<input
							id="edit-start-time"
							type="time"
							class="input input-bordered"
							bind:value={editStartTime}
							disabled
							aria-label="Start time (read-only)"
						/>
						<label class="label">
							<span class="label-text-alt text-base-content/60">Start time cannot be changed</span>
						</label>
					</div>
					{#if editEntry.endedAt}
						<div class="form-control">
							<label class="label" for="edit-end-time">
								<span class="label-text font-semibold">End Time</span>
							</label>
							<input
								id="edit-end-time"
								type="time"
								class="input input-bordered"
								bind:value={editEndTime}
								aria-label="End time"
							/>
							<label class="label">
								<span class="label-text-alt">Adjust the end time to change duration</span>
							</label>
						</div>
					{:else}
						<div class="form-control">
							<label class="label">
								<span class="label-text font-semibold">End Time</span>
							</label>
							<div class="text-base-content/60">Entry is still active</div>
							<label class="label">
								<span class="label-text-alt">Cannot edit active entries</span>
							</label>
						</div>
					{/if}
				</div>

				<!-- Duration Preview -->
				{#if editEntry.endedAt && editEndTime}
					{@const previewDuration = getPreviewDuration()}
					{@const adjustment = calculateAdjustment()}
					<div class="alert {adjustment !== 0 ? 'alert-info' : 'alert-warning'}">
						<div class="flex flex-col gap-2 w-full">
							<div class="flex justify-between items-center">
								<span class="font-semibold">New Duration:</span>
								<span class="text-lg">{previewDuration}</span>
							</div>
							{#if adjustment !== 0}
								<div class="flex justify-between items-center text-sm">
									<span>Adjustment:</span>
									<span class="{adjustment > 0 ? 'text-success' : 'text-error'}">
										{adjustment > 0 ? '+' : ''}{adjustment} minutes
									</span>
								</div>
							{:else}
								<div class="text-sm text-center">
									No changes made
								</div>
							{/if}
						</div>
					</div>
				{/if}

				<!-- Error Display -->
				{#if editError}
					<div class="alert alert-error">
						<span>{editError}</span>
					</div>
				{/if}
			</div>

			<!-- Modal Actions -->
			<div class="modal-action">
				<button
					class="btn btn-ghost"
					onclick={closeEditModal}
					disabled={editLoading}
				>
					Cancel
				</button>
				<button
					class="btn btn-primary"
					onclick={saveEdit}
					disabled={editLoading || !editEntry?.endedAt || calculateAdjustment() === 0}
				>
					{editLoading ? 'Saving...' : 'Save Changes'}
				</button>
			</div>
		</div>
		<button
			class="modal-backdrop"
			onclick={closeEditModal}
			aria-label="Close modal"
			tabindex="-1"
		></button>
	</div>
{/if}

<!-- Delete Modal -->
{#if deleteModalOpen && deleteEntry}
	<div class="modal modal-open" role="dialog" aria-labelledby="delete-modal-title" aria-modal="true">
		<div class="modal-box">
			<h3 id="delete-modal-title" class="font-bold text-lg mb-4">Delete Entry</h3>

			<p class="mb-4">Are you sure you want to delete this entry? This action cannot be undone.</p>

			<!-- Entry Details -->
			<div class="bg-base-300 p-4 rounded-lg space-y-2">
				<div class="flex justify-between">
					<span class="font-semibold">Type:</span>
					<span class="badge {getStateBadge(deleteEntry.state)}">
						{getStateName(deleteEntry.state)}
					</span>
				</div>
				<div class="flex justify-between">
					<span class="font-semibold">Start:</span>
					<span>{formatDate(deleteEntry.startedAt)}</span>
				</div>
				<div class="flex justify-between">
					<span class="font-semibold">End:</span>
					<span>{formatDate(deleteEntry.endedAt)}</span>
				</div>
				<div class="flex justify-between">
					<span class="font-semibold">Duration:</span>
					<span>{formatDurationDisplay(deleteEntry.durationHours)}</span>
				</div>
			</div>

			<!-- Error Display -->
			{#if deleteError}
				<div class="alert alert-error mt-4">
					<span>{deleteError}</span>
				</div>
			{/if}

			<!-- Modal Actions -->
			<div class="modal-action">
				<button
					class="btn btn-ghost"
					onclick={closeDeleteModal}
					disabled={deleteLoading}
				>
					Cancel
				</button>
				<button
					class="btn btn-error"
					onclick={confirmDelete}
					disabled={deleteLoading}
				>
					{deleteLoading ? 'Deleting...' : 'Delete'}
				</button>
			</div>
		</div>
		<button
			class="modal-backdrop"
			onclick={closeDeleteModal}
			aria-label="Close modal"
			tabindex="-1"
		></button>
	</div>
{/if}
