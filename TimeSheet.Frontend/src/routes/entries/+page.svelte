<script lang="ts">
	import { onMount } from 'svelte';
	import { apiClient, type DailyBreakdownDto, DailyBreakdownDto as DailyBreakdownDtoClass } from '$lib/api';

	// Grouping options
	type GroupingLevel = 'day' | 'week' | 'month' | 'year';
	let selectedGrouping: GroupingLevel = $state('day');

	// Date range filtering
	let startDate = $state('');
	let endDate = $state('');

	// Pagination
	let currentPage = $state(1);
	let pageSize = $state(30);
	let totalPages = $state(1);

	// Data
	let entries: DailyBreakdownDto[] = $state([]);
	let loading = $state(false);
	let error = $state('');

	// Summary calculations
	let summary = $derived.by(() => {
		const activeEntries = entries.filter(e => e.hasActivity);
		const totalCount = activeEntries.length;

		if (totalCount === 0) {
			return {
				avgWork: 0,
				avgCommuteToWork: 0,
				avgLunch: 0,
				avgCommuteToHome: 0,
				avgTotal: 0,
				totalWork: 0,
				totalCommuteToWork: 0,
				totalLunch: 0,
				totalCommuteToHome: 0,
				totalDuration: 0,
				activeDays: 0
			};
		}

		const totalWork = activeEntries.reduce((sum, e) => sum + e.workHours, 0);
		const totalCommuteToWork = activeEntries.reduce((sum, e) => sum + e.commuteToWorkHours, 0);
		const totalLunch = activeEntries.reduce((sum, e) => sum + e.lunchHours, 0);
		const totalCommuteToHome = activeEntries.reduce((sum, e) => sum + e.commuteToHomeHours, 0);
		const totalDuration = activeEntries.reduce((sum, e) => sum + (e.totalDurationHours || 0), 0);

		return {
			avgWork: totalWork / totalCount,
			avgCommuteToWork: totalCommuteToWork / totalCount,
			avgLunch: totalLunch / totalCount,
			avgCommuteToHome: totalCommuteToHome / totalCount,
			avgTotal: totalDuration / totalCount,
			totalWork,
			totalCommuteToWork,
			totalLunch,
			totalCommuteToHome,
			totalDuration,
			activeDays: totalCount
		};
	});

	// Initialize date range (last 30 days by default)
	onMount(() => {
		const today = new Date();
		const thirtyDaysAgo = new Date(today);
		thirtyDaysAgo.setDate(today.getDate() - 30);

		endDate = today.toISOString().split('T')[0];
		startDate = thirtyDaysAgo.toISOString().split('T')[0];

		loadData();
	});

	async function loadData() {
		loading = true;
		error = '';

		try {
			const start = startDate ? new Date(startDate + 'T00:00:00') : undefined;
			const end = endDate ? new Date(endDate + 'T23:59:59') : undefined;

			// For day grouping, use the daily breakdown endpoint
			if (selectedGrouping === 'day') {
				const result = await apiClient.dailyBreakdown(start, end);
				entries = result;

				// Calculate pagination based on results
				const totalItems = result.length;
				totalPages = Math.ceil(totalItems / pageSize);

				// Apply client-side pagination
				const startIndex = (currentPage - 1) * pageSize;
				const endIndex = startIndex + pageSize;
				entries = result.slice(startIndex, endIndex);
			} else {
				// For other groupings, we'll aggregate the daily data
				const allData = await apiClient.dailyBreakdown(start, end);
				entries = aggregateByGrouping(allData, selectedGrouping);

				const totalItems = entries.length;
				totalPages = Math.ceil(totalItems / pageSize);

				// Apply client-side pagination
				const startIndex = (currentPage - 1) * pageSize;
				const endIndex = startIndex + pageSize;
				entries = entries.slice(startIndex, endIndex);
			}
		} catch (e) {
			error = e instanceof Error ? e.message : 'Failed to load data';
			console.error('Error loading entries:', e);
		} finally {
			loading = false;
		}
	}

	function aggregateByGrouping(data: DailyBreakdownDto[], grouping: GroupingLevel): DailyBreakdownDto[] {
		if (grouping === 'day') return data;

		const groups = new Map<string, DailyBreakdownDto[]>();

		for (const entry of data) {
			const date = new Date(entry.date);
			let key: string;

			switch (grouping) {
				case 'week':
					// ISO week number
					const weekStart = getWeekStart(date);
					key = weekStart.toISOString().split('T')[0];
					break;
				case 'month':
					key = `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}`;
					break;
				case 'year':
					key = `${date.getFullYear()}`;
					break;
			}

			if (!groups.has(key)) {
				groups.set(key, []);
			}
			groups.get(key)!.push(entry);
		}

		// Aggregate each group
		const result: DailyBreakdownDto[] = [];
		for (const [key, groupEntries] of groups) {
			const hasActivity = groupEntries.some(e => e.hasActivity);
			const workHours = groupEntries.reduce((sum, e) => sum + e.workHours, 0);
			const commuteToWorkHours = groupEntries.reduce((sum, e) => sum + e.commuteToWorkHours, 0);
			const commuteToHomeHours = groupEntries.reduce((sum, e) => sum + e.commuteToHomeHours, 0);
			const lunchHours = groupEntries.reduce((sum, e) => sum + e.lunchHours, 0);
			const totalDurationHours = groupEntries.reduce((sum, e) => sum + (e.totalDurationHours || 0), 0);

			const dto = new DailyBreakdownDtoClass();
			dto.date = new Date(groupEntries[0].date);
			dto.workHours = workHours;
			dto.commuteToWorkHours = commuteToWorkHours;
			dto.commuteToHomeHours = commuteToHomeHours;
			dto.lunchHours = lunchHours;
			dto.totalDurationHours = totalDurationHours;
			dto.hasActivity = hasActivity;
			result.push(dto);
		}

		return result.sort((a, b) => b.date.getTime() - a.date.getTime());
	}

	function getWeekStart(date: Date): Date {
		const d = new Date(date);
		const day = d.getDay();
		const diff = d.getDate() - day + (day === 0 ? -6 : 1); // Adjust for Monday start
		return new Date(d.setDate(diff));
	}

	function formatHours(hours: number): string {
		if (hours === 0) return '-';
		const h = Math.floor(hours);
		const m = Math.round((hours - h) * 60);
		return m > 0 ? `${h}h ${m}m` : `${h}h`;
	}

	function formatDate(date: Date, grouping: GroupingLevel): string {
		const d = new Date(date);

		switch (grouping) {
			case 'day':
				return d.toLocaleDateString('en-US', {
					year: 'numeric',
					month: 'short',
					day: 'numeric',
					weekday: 'short'
				});
			case 'week':
				const weekEnd = new Date(d);
				weekEnd.setDate(d.getDate() + 6);
				return `${d.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })} - ${weekEnd.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}`;
			case 'month':
				return d.toLocaleDateString('en-US', { year: 'numeric', month: 'long' });
			case 'year':
				return d.getFullYear().toString();
			default:
				return d.toLocaleDateString();
		}
	}

	function handleGroupingChange(newGrouping: GroupingLevel) {
		selectedGrouping = newGrouping;
		currentPage = 1; // Reset to first page
		loadData();
	}

	function handleFilterApply() {
		currentPage = 1; // Reset to first page
		loadData();
	}

	function nextPage() {
		if (currentPage < totalPages) {
			currentPage++;
			loadData();
		}
	}

	function previousPage() {
		if (currentPage > 1) {
			currentPage--;
			loadData();
		}
	}
</script>

<div class="max-w-7xl mx-auto">
	<h1 class="text-3xl font-bold mb-6">Audit Table</h1>

	<!-- Grouping Controls -->
	<div class="card bg-base-200 shadow-xl mb-6">
		<div class="card-body">
			<div class="flex flex-wrap gap-4 items-center">
				<div class="flex items-center gap-2">
					<svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
						<path stroke-linecap="round" stroke-linejoin="round" d="M12 3c2.755 0 5.455.232 8.083.678.533.09.917.556.917 1.096v1.044a2.25 2.25 0 01-.659 1.591l-5.432 5.432a2.25 2.25 0 00-.659 1.591v2.927a2.25 2.25 0 01-1.244 2.013L9.75 21v-6.568a2.25 2.25 0 00-.659-1.591L3.659 7.409A2.25 2.25 0 013 5.818V4.774c0-.54.384-1.006.917-1.096A48.32 48.32 0 0112 3z" />
					</svg>
					<span class="font-semibold">Group by:</span>
				</div>
				<div class="join">
					<button
						class="join-item btn btn-sm {selectedGrouping === 'day' ? 'btn-primary' : 'btn-ghost'}"
						onclick={() => handleGroupingChange('day')}
					>
						Day
					</button>
					<button
						class="join-item btn btn-sm {selectedGrouping === 'week' ? 'btn-primary' : 'btn-ghost'}"
						onclick={() => handleGroupingChange('week')}
					>
						Week
					</button>
					<button
						class="join-item btn btn-sm {selectedGrouping === 'month' ? 'btn-primary' : 'btn-ghost'}"
						onclick={() => handleGroupingChange('month')}
					>
						Month
					</button>
					<button
						class="join-item btn btn-sm {selectedGrouping === 'year' ? 'btn-primary' : 'btn-ghost'}"
						onclick={() => handleGroupingChange('year')}
					>
						Year
					</button>
				</div>
			</div>

			<!-- Date Range Filter -->
			<div class="divider"></div>
			<div class="flex flex-wrap gap-4 items-end">
				<div class="flex items-center gap-2">
					<svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
						<path stroke-linecap="round" stroke-linejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 012.25-2.25h13.5A2.25 2.25 0 0121 7.5v11.25m-18 0A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75m-18 0v-7.5A2.25 2.25 0 015.25 9h13.5A2.25 2.25 0 0121 11.25v7.5" />
					</svg>
					<span class="font-semibold">Date Range:</span>
				</div>
				<div class="form-control">
					<label class="label" for="start-date">
						<span class="label-text">Start Date</span>
					</label>
					<input
						id="start-date"
						type="date"
						class="input input-bordered input-sm"
						bind:value={startDate}
					/>
				</div>
				<div class="form-control">
					<label class="label" for="end-date">
						<span class="label-text">End Date</span>
					</label>
					<input
						id="end-date"
						type="date"
						class="input input-bordered input-sm"
						bind:value={endDate}
					/>
				</div>
				<button
					class="btn btn-primary btn-sm"
					onclick={handleFilterApply}
					disabled={loading}
				>
					{loading ? 'Loading...' : 'Apply Filter'}
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

	<!-- Summary Card -->
	<div class="card bg-base-300 shadow-xl mb-6">
		<div class="card-body">
			<h2 class="card-title text-lg">Summary</h2>
			<div class="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-4 text-sm">
				<div>
					<div class="text-base-content/70">Active Days</div>
					<div class="text-xl font-bold">{summary.activeDays}</div>
				</div>
				<div>
					<div class="text-base-content/70">Avg Work</div>
					<div class="text-xl font-bold">{formatHours(summary.avgWork)}</div>
				</div>
				<div>
					<div class="text-base-content/70">Avg Commute (to work)</div>
					<div class="text-xl font-bold">{formatHours(summary.avgCommuteToWork)}</div>
				</div>
				<div>
					<div class="text-base-content/70">Avg Lunch</div>
					<div class="text-xl font-bold">{formatHours(summary.avgLunch)}</div>
				</div>
				<div>
					<div class="text-base-content/70">Avg Commute (to home)</div>
					<div class="text-xl font-bold">{formatHours(summary.avgCommuteToHome)}</div>
				</div>
				<div>
					<div class="text-base-content/70">Avg Total</div>
					<div class="text-xl font-bold">{formatHours(summary.avgTotal)}</div>
				</div>
			</div>
		</div>
	</div>

	<!-- Data Table -->
	<div class="card bg-base-200 shadow-xl mb-6">
		<div class="card-body overflow-x-auto">
			{#if loading}
				<div class="flex justify-center items-center py-12">
					<span class="loading loading-spinner loading-lg"></span>
				</div>
			{:else if entries.length === 0}
				<div class="text-center py-12 text-base-content/70">
					<p class="text-lg">No entries found for the selected date range.</p>
				</div>
			{:else}
				<table class="table table-zebra table-pin-rows">
					<thead>
						<tr>
							<th>Date / Period</th>
							<th class="text-right">Commute (to work)</th>
							<th class="text-right">Work</th>
							<th class="text-right">Lunch</th>
							<th class="text-right">Commute (to home)</th>
							<th class="text-right">Total</th>
						</tr>
					</thead>
					<tbody>
						{#each entries as entry}
							<tr class:opacity-40={!entry.hasActivity}>
								<td class="font-medium">
									{formatDate(entry.date, selectedGrouping)}
									{#if !entry.hasActivity}
										<span class="badge badge-ghost badge-sm ml-2">No activity</span>
									{/if}
								</td>
								<td class="text-right">{formatHours(entry.commuteToWorkHours)}</td>
								<td class="text-right">{formatHours(entry.workHours)}</td>
								<td class="text-right">{formatHours(entry.lunchHours)}</td>
								<td class="text-right">{formatHours(entry.commuteToHomeHours)}</td>
								<td class="text-right font-semibold">{formatHours(entry.totalDurationHours || 0)}</td>
							</tr>
						{/each}
					</tbody>
					<tfoot>
						<tr class="bg-base-300 font-bold">
							<td>Totals (displayed rows)</td>
							<td class="text-right">{formatHours(entries.reduce((sum, e) => sum + e.commuteToWorkHours, 0))}</td>
							<td class="text-right">{formatHours(entries.reduce((sum, e) => sum + e.workHours, 0))}</td>
							<td class="text-right">{formatHours(entries.reduce((sum, e) => sum + e.lunchHours, 0))}</td>
							<td class="text-right">{formatHours(entries.reduce((sum, e) => sum + e.commuteToHomeHours, 0))}</td>
							<td class="text-right">{formatHours(entries.reduce((sum, e) => sum + (e.totalDurationHours || 0), 0))}</td>
						</tr>
					</tfoot>
				</table>
			{/if}
		</div>
	</div>

	<!-- Pagination -->
	{#if totalPages > 1}
		<div class="flex justify-center items-center gap-4 mb-6">
			<button
				class="btn btn-sm"
				onclick={previousPage}
				disabled={currentPage === 1 || loading}
			>
				<svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
					<path stroke-linecap="round" stroke-linejoin="round" d="M15.75 19.5L8.25 12l7.5-7.5" />
				</svg>
				Previous
			</button>
			<span class="text-sm">
				Page {currentPage} of {totalPages}
			</span>
			<button
				class="btn btn-sm"
				onclick={nextPage}
				disabled={currentPage === totalPages || loading}
			>
				Next
				<svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
					<path stroke-linecap="round" stroke-linejoin="round" d="M8.25 4.5l7.5 7.5-7.5 7.5" />
				</svg>
			</button>
		</div>
	{/if}
</div>
