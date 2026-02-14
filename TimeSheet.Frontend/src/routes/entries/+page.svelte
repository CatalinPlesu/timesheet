<script lang="ts">
	import { onMount } from 'svelte';
	import { apiClient, type TrackingEntryDto, type EntryListResponse, EntryUpdateRequest } from '$lib/api';

	// Icons (Heroicons - outline)
	const PencilSquareIcon = `<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-5 h-5">
		<path stroke-linecap="round" stroke-linejoin="round" d="m16.862 4.487 1.687-1.688a1.875 1.875 0 1 1 2.652 2.652L10.582 16.07a4.5 4.5 0 0 1-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 0 1 1.13-1.897l8.932-8.931Zm0 0L19.5 7.125M18 14v4.75A2.25 2.25 0 0 1 15.75 21H5.25A2.25 2.25 0 0 1 3 18.75V8.25A2.25 2.25 0 0 1 5.25 6H10" />
	</svg>`;

	const TrashIcon = `<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-5 h-5">
		<path stroke-linecap="round" stroke-linejoin="round" d="m14.74 9-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 0 1-2.244 2.077H8.084a2.25 2.25 0 0 1-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 0 0-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 0 1 3.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 0 0-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 0 0-7.5 0" />
	</svg>`;

	// State
	let entries: TrackingEntryDto[] = $state([]);
	let totalCount = $state(0);
	let page = $state(1);
	let pageSize = $state(50);
	let totalPages = $state(0);
	let groupBy = $state(0); // 0=None, 1=Day, 2=Week, 3=Month, 4=Year
	let loading = $state(false);
	let error = $state<string | null>(null);

	// Date range filters
	let startDate = $state<Date | undefined>(undefined);
	let endDate = $state<Date | undefined>(undefined);

	// Edit modal state
	let editModalOpen = $state(false);
	let editEntry = $state<TrackingEntryDto | null>(null);
	let adjustmentMinutes = $state(0);
	let editError = $state<string | null>(null);
	let editLoading = $state(false);

	// Delete modal state
	let deleteModalOpen = $state(false);
	let deleteEntry = $state<TrackingEntryDto | null>(null);
	let deleteError = $state<string | null>(null);
	let deleteLoading = $state(false);

	// Fetch entries from API
	async function fetchEntries() {
		loading = true;
		error = null;
		try {
			const response = await apiClient.entriesGET(startDate, endDate, groupBy, page, pageSize);
			entries = response.entries;
			totalCount = response.totalCount;
			totalPages = response.totalPages;
		} catch (err: any) {
			error = err.message || 'Failed to load entries';
			console.error('Failed to fetch entries:', err);
		} finally {
			loading = false;
		}
	}

	// Format date for display
	function formatDate(date: Date | null | undefined): string {
		if (!date) return 'N/A';
		return new Date(date).toLocaleString('en-US', {
			year: 'numeric',
			month: 'short',
			day: 'numeric',
			hour: '2-digit',
			minute: '2-digit'
		});
	}

	// Format duration
	function formatDuration(hours: number | null | undefined): string {
		if (hours === null || hours === undefined) return 'Active';
		const h = Math.floor(hours);
		const m = Math.round((hours - h) * 60);
		return `${h}h ${m}m`;
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

	// Open edit modal
	function openEditModal(entry: TrackingEntryDto) {
		editEntry = entry;
		adjustmentMinutes = 0;
		editError = null;
		editModalOpen = true;
	}

	// Close edit modal
	function closeEditModal() {
		editModalOpen = false;
		editEntry = null;
		adjustmentMinutes = 0;
		editError = null;
	}

	// Handle edit save
	async function saveEdit() {
		if (!editEntry) return;

		editLoading = true;
		editError = null;
		try {
			const request = new EntryUpdateRequest({
				adjustmentMinutes: adjustmentMinutes
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
			editError = err.message || 'Failed to update entry';
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
			deleteError = err.message || 'Failed to delete entry';
			console.error('Failed to delete entry:', err);
		} finally {
			deleteLoading = false;
		}
	}

	// Quick adjustment buttons
	function adjustBy(minutes: number) {
		adjustmentMinutes += minutes;
	}

	// Calculate adjusted times for preview
	function getAdjustedTimes(entry: TrackingEntryDto, adjustment: number) {
		if (!entry) return { start: '', end: '', duration: '' };

		const startTime = new Date(entry.startedAt);
		const adjustedStart = new Date(startTime.getTime() + adjustment * 60000);

		let adjustedEnd = '';
		let duration = '';

		if (entry.endedAt) {
			const endTime = new Date(entry.endedAt);
			const adjustedEndTime = new Date(endTime.getTime() + adjustment * 60000);
			adjustedEnd = formatDate(adjustedEndTime);

			const durationMs = adjustedEndTime.getTime() - adjustedStart.getTime();
			const durationHours = durationMs / (1000 * 60 * 60);
			duration = formatDuration(durationHours);
		} else {
			adjustedEnd = 'Active';
			duration = 'Active';
		}

		return {
			start: formatDate(adjustedStart),
			end: adjustedEnd,
			duration: duration
		};
	}

	// Handle keyboard events for modals
	function handleEscapeKey(event: KeyboardEvent) {
		if (event.key === 'Escape') {
			if (editModalOpen) closeEditModal();
			if (deleteModalOpen) closeDeleteModal();
		}
	}

	// Load entries on mount
	onMount(() => {
		fetchEntries();
	});
</script>

<svelte:window onkeydown={handleEscapeKey} />

<div class="max-w-7xl mx-auto">
	<h1 class="text-3xl font-bold mb-6">Audit Table</h1>

	<!-- Filters and Controls -->
	<div class="card bg-base-200 shadow-xl mb-6">
		<div class="card-body">
			<div class="flex flex-col sm:flex-row gap-4 items-start sm:items-end">
				<!-- Group By Select -->
				<div class="form-control w-full sm:w-48">
					<label class="label" for="groupBy">
						<span class="label-text">Group By</span>
					</label>
					<select
						id="groupBy"
						class="select select-bordered"
						bind:value={groupBy}
						onchange={() => fetchEntries()}
					>
						<option value={0}>None</option>
						<option value={1}>Day</option>
						<option value={2}>Week</option>
						<option value={3}>Month</option>
						<option value={4}>Year</option>
					</select>
				</div>

				<!-- Refresh Button -->
				<button
					class="btn btn-primary"
					onclick={() => fetchEntries()}
					disabled={loading}
				>
					{loading ? 'Loading...' : 'Refresh'}
				</button>
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
		<div class="card-body">
			<h2 class="card-title">Time Entries</h2>

			{#if loading}
				<div class="flex justify-center items-center py-12">
					<span class="loading loading-spinner loading-lg"></span>
				</div>
			{:else if entries.length === 0}
				<div class="text-center py-12 text-base-content/70">
					<p>No entries found</p>
				</div>
			{:else}
				<div class="overflow-x-auto">
					<table class="table table-zebra">
						<thead>
							<tr>
								<th>Type</th>
								<th>Start Time</th>
								<th>End Time</th>
								<th>Duration</th>
								<th>Status</th>
								<th class="text-right">Actions</th>
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
									<td>{formatDuration(entry.durationHours)}</td>
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
				<div class="flex justify-between items-center mt-4">
					<div class="text-sm text-base-content/70">
						Showing {entries.length} of {totalCount} entries
					</div>
					<div class="text-sm text-base-content/70">
						Page {page} of {totalPages}
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

				<div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
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
				</div>

				<div>
					<div class="label">
						<span class="label-text">Duration</span>
					</div>
					<div class="text-base-content">{formatDuration(editEntry.durationHours)}</div>
				</div>

				<div class="divider">Adjustment</div>

				<!-- Adjustment Controls -->
				<div class="form-control">
					<label class="label" for="adjustment-input">
						<span class="label-text">Adjust by (minutes)</span>
					</label>
					<div class="flex gap-2 flex-wrap">
						<button class="btn btn-sm" onclick={() => adjustBy(-30)}>-30m</button>
						<button class="btn btn-sm" onclick={() => adjustBy(-15)}>-15m</button>
						<button class="btn btn-sm" onclick={() => adjustBy(-5)}>-5m</button>
						<button class="btn btn-sm" onclick={() => adjustBy(-1)}>-1m</button>
						<input
							id="adjustment-input"
							type="number"
							class="input input-sm input-bordered w-24 text-center"
							bind:value={adjustmentMinutes}
							aria-label="Adjustment minutes"
						/>
						<button class="btn btn-sm" onclick={() => adjustBy(1)}>+1m</button>
						<button class="btn btn-sm" onclick={() => adjustBy(5)}>+5m</button>
						<button class="btn btn-sm" onclick={() => adjustBy(15)}>+15m</button>
						<button class="btn btn-sm" onclick={() => adjustBy(30)}>+30m</button>
					</div>
					<div class="label">
						<span class="label-text-alt">Current adjustment: {adjustmentMinutes} minutes</span>
					</div>
				</div>

				{#if adjustmentMinutes !== 0}
					<div class="divider">Preview</div>

					{@const adjusted = getAdjustedTimes(editEntry, adjustmentMinutes)}
					<div class="grid grid-cols-1 sm:grid-cols-2 gap-4 bg-base-300 p-4 rounded-lg">
						<div>
							<div class="label">
								<span class="label-text font-semibold">New Start Time</span>
							</div>
							<div class="text-base-content">{adjusted.start}</div>
						</div>
						<div>
							<div class="label">
								<span class="label-text font-semibold">New End Time</span>
							</div>
							<div class="text-base-content">{adjusted.end}</div>
						</div>
						<div class="sm:col-span-2">
							<div class="label">
								<span class="label-text font-semibold">New Duration</span>
							</div>
							<div class="text-base-content">{adjusted.duration}</div>
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
					disabled={editLoading || adjustmentMinutes === 0}
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
					<span>{formatDuration(deleteEntry.durationHours)}</span>
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
