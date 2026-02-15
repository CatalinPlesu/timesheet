<script lang="ts">
	import { onMount, onDestroy } from 'svelte';
	import {
		apiClient,
		type CurrentStateResponse,
		TrackingStateRequest,
		TrackingStateWithOffsetRequest
	} from '$lib/api';
	import { auth } from '$lib/stores/auth';
	import { extractErrorMessage } from '$lib/utils/errorHandling';
	import { formatLocalTime } from '$lib/utils/timeFormatter';

	// State enum values (must match backend)
	const TrackingState = {
		Idle: 0,
		Commuting: 1,
		Working: 2,
		Lunch: 3
	} as const;

	type TrackingState = (typeof TrackingState)[keyof typeof TrackingState];

	const CommuteDirection = {
		ToWork: 0,
		ToHome: 1
	} as const;

	// Component state
	let currentState = $state<CurrentStateResponse | null>(null);
	let loading = $state(false);
	let error = $state<string | null>(null);
	let toast = $state<{ message: string; type: 'success' | 'error' } | null>(null);
	let elapsedSeconds = $state(0);
	let showTimeOffsetMenu = $state<TrackingState | null>(null);
	let customTime = $state('');

	// Intervals
	let pollInterval: number | undefined;
	let timerInterval: number | undefined;

	// Load current state from API
	async function loadCurrentState() {
		if (!$auth.isAuthenticated) {
			error = 'Please log in to use tracking features';
			return;
		}

		try {
			loading = true;
			error = null;
			currentState = await apiClient.current();
			updateElapsedTime();
		} catch (err) {
			error = extractErrorMessage(err, 'Failed to load current state');
			console.error('Failed to load current state:', err);
		} finally {
			loading = false;
		}
	}

	// Calculate elapsed time
	function updateElapsedTime() {
		if (!currentState?.startedAt) {
			elapsedSeconds = 0;
			return;
		}

		// Both times are in UTC, so we can directly compare them
		const now = new Date();
		const started = new Date(currentState.startedAt);
		elapsedSeconds = Math.floor((now.getTime() - started.getTime()) / 1000);
	}

	// Format elapsed time as HH:MM:SS
	function formatElapsedTime(seconds: number): string {
		const hours = Math.floor(seconds / 3600);
		const minutes = Math.floor((seconds % 3600) / 60);
		const secs = seconds % 60;
		return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
	}

	// Format start time in user's local timezone (using shared utility)
	function formatStartTime(utcTime: Date | string): string {
		// Use reactive reference to auth store so it updates when utcOffsetMinutes changes
		return formatLocalTime(utcTime, $auth.utcOffsetMinutes ?? 0);
	}

	// Show toast notification
	function showToast(message: string, type: 'success' | 'error' = 'success') {
		toast = { message, type };
		setTimeout(() => {
			toast = null;
		}, 3000);
	}

	// Toggle tracking state (no offset)
	async function toggleState(state: TrackingState) {
		if (loading) return;

		try {
			loading = true;
			error = null;

			const request = new TrackingStateRequest({ state: state });
			const response = await apiClient.toggle(request);

			showToast(response.message || 'State updated successfully');
			await loadCurrentState();
		} catch (err) {
			const errorMessage = extractErrorMessage(err, 'Failed to toggle state');
			error = errorMessage;
			showToast(errorMessage, 'error');
			console.error('Failed to toggle state:', err);
		} finally {
			loading = false;
		}
	}

	// Toggle tracking state with time offset
	async function toggleStateWithOffset(state: TrackingState, offsetMinutes: number) {
		if (loading) return;

		try {
			loading = true;
			error = null;

			const request = new TrackingStateWithOffsetRequest({
				state: state,
				offsetMinutes: offsetMinutes
			});

			const response = await apiClient.toggleWithOffset(request);

			showToast(response.message || 'State updated successfully');
			await loadCurrentState();
			showTimeOffsetMenu = null;
			customTime = '';
		} catch (err) {
			const errorMessage = err instanceof Error ? err.message : 'Failed to toggle state';
			error = errorMessage;
			showToast(errorMessage, 'error');
			console.error('Failed to toggle state with offset:', err);
		} finally {
			loading = false;
		}
	}

	// Parse time offset input (supports multiple formats)
	function parseTimeOffset(input: string): number | null {
		const trimmed = input.trim();

		// Format 1: Relative minutes (+30m, -30m, 30m)
		const minutesMatch = trimmed.match(/^([+-]?\d+)m$/i);
		if (minutesMatch) {
			return parseInt(minutesMatch[1]);
		}

		// Format 2: Relative hours (+2h, -2h, 2h)
		const hoursMatch = trimmed.match(/^([+-]?\d+)h$/i);
		if (hoursMatch) {
			return parseInt(hoursMatch[1]) * 60;
		}

		// Format 3: Absolute time (HH:MM)
		const timeMatch = trimmed.match(/^(\d{1,2}):(\d{2})$/);
		if (timeMatch) {
			const hours = parseInt(timeMatch[1]);
			const minutes = parseInt(timeMatch[2]);

			if (hours < 0 || hours > 23 || minutes < 0 || minutes > 59) {
				return null; // Invalid time
			}

			// Calculate offset from now (in user's local timezone)
			const now = new Date();
			const utcOffsetMinutes = $auth.utcOffsetMinutes ?? 0;

			// Get current time in user's timezone
			const nowLocal = new Date(now.getTime() + utcOffsetMinutes * 60 * 1000);
			const currentHours = nowLocal.getUTCHours();
			const currentMinutes = nowLocal.getUTCMinutes();

			// Calculate target time in minutes since midnight
			const targetMinutes = hours * 60 + minutes;
			const currentTotalMinutes = currentHours * 60 + currentMinutes;

			// Calculate offset (negative because we're going back in time)
			let offsetMinutes = targetMinutes - currentTotalMinutes;

			// If target is in the future, assume it was yesterday
			if (offsetMinutes > 0) {
				offsetMinutes -= 24 * 60; // Go back 24 hours
			}

			return offsetMinutes;
		}

		return null; // Unrecognized format
	}

	// Handle custom time input
	function handleCustomTime(state: TrackingState) {
		if (!customTime) return;

		const offsetMinutes = parseTimeOffset(customTime);

		if (offsetMinutes === null) {
			showToast(
				'Invalid format. Use: +30m, -30m, +2h, -2h, or HH:MM (e.g., 14:30)',
				'error'
			);
			return;
		}

		toggleStateWithOffset(state, offsetMinutes);
	}

	// Handle button click (simple toggle)
	function handleButtonClick(state: TrackingState) {
		toggleState(state);
	}

	// Handle context menu (right-click) to show time offset menu
	function handleContextMenu(event: MouseEvent, state: TrackingState) {
		event.preventDefault();
		showTimeOffsetMenu = state;
	}

	// Handle long press (for mobile)
	let pressTimer: number | undefined;
	let pressState: TrackingState | null = null;

	function handleTouchStart(state: TrackingState) {
		pressState = state;
		pressTimer = window.setTimeout(() => {
			showTimeOffsetMenu = state;
		}, 500); // 500ms long press
	}

	function handleTouchEnd() {
		if (pressTimer) {
			clearTimeout(pressTimer);
			pressTimer = undefined;
		}
		if (pressState !== null && showTimeOffsetMenu === null) {
			handleButtonClick(pressState);
		}
		pressState = null;
	}

	// Get state label
	function getStateLabel(state: number): string {
		switch (state) {
			case TrackingState.Idle:
				return 'Idle';
			case TrackingState.Commuting:
				return 'Commuting';
			case TrackingState.Working:
				return 'Working';
			case TrackingState.Lunch:
				return 'Lunch';
			default:
				return 'Unknown';
		}
	}

	// Get commute direction label
	function getCommuteDirectionLabel(direction: number | null | undefined): string {
		if (direction === null || direction === undefined) return '';
		return direction === CommuteDirection.ToWork ? 'To Work' : 'To Home';
	}

	// Check if state is active
	function isStateActive(state: TrackingState): boolean {
		return currentState?.state === state;
	}

	// Get button classes
	function getButtonClasses(state: TrackingState): string {
		const baseClasses = 'btn btn-lg flex-1 min-h-[96px] flex flex-col gap-2 transition-all';
		const active = isStateActive(state);

		if (state === TrackingState.Commuting) {
			return `${baseClasses} ${active ? 'bg-orange-500 hover:bg-orange-600 text-white border-orange-500 shadow-xl scale-105' : 'btn-outline border-orange-500 text-orange-500 hover:bg-orange-500 hover:text-white opacity-70 hover:opacity-100'}`;
		} else if (state === TrackingState.Working) {
			return `${baseClasses} ${active ? 'bg-blue-500 hover:bg-blue-600 text-white border-blue-500 shadow-xl scale-105' : 'btn-outline border-blue-500 text-blue-500 hover:bg-blue-500 hover:text-white opacity-70 hover:opacity-100'}`;
		} else if (state === TrackingState.Lunch) {
			return `${baseClasses} ${active ? 'bg-green-500 hover:bg-green-600 text-white border-green-500 shadow-xl scale-105' : 'btn-outline border-green-500 text-green-500 hover:bg-green-500 hover:text-white opacity-70 hover:opacity-100'}`;
		}

		return baseClasses;
	}

	// Lifecycle
	onMount(() => {
		loadCurrentState();

		// Poll every 5 seconds
		pollInterval = window.setInterval(() => {
			loadCurrentState();
		}, 5000);

		// Update timer every second
		timerInterval = window.setInterval(() => {
			if (currentState?.startedAt) {
				updateElapsedTime();
			}
		}, 1000);
	});

	onDestroy(() => {
		if (pollInterval) clearInterval(pollInterval);
		if (timerInterval) clearInterval(timerInterval);
	});
</script>

<div class="max-w-4xl mx-auto px-4">
	<div class="mb-8">
		<h1 class="text-4xl font-bold mb-2">Tracking Controls</h1>
		<p class="text-base-content/70">Monitor and control your work time tracking</p>
	</div>

	<!-- Current Status Card -->
	<div class="card bg-base-200 shadow-xl mb-6">
		<div class="card-body p-6">
			<h2 class="card-title text-xl mb-4">
				<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-6 h-6">
					<path stroke-linecap="round" stroke-linejoin="round" d="M11.25 11.25l.041-.02a.75.75 0 011.063.852l-.708 2.836a.75.75 0 001.063.853l.041-.021M21 12a9 9 0 11-18 0 9 9 0 0118 0zm-9-3.75h.008v.008H12V8.25z" />
				</svg>
				Current Status
			</h2>

			{#if loading && !currentState}
				<div class="flex items-center gap-2">
					<span class="loading loading-spinner loading-sm"></span>
					<span>Loading...</span>
				</div>
			{:else if error && !currentState}
				<div class="alert alert-error">
					<svg
						xmlns="http://www.w3.org/2000/svg"
						class="h-6 w-6 shrink-0 stroke-current"
						fill="none"
						viewBox="0 0 24 24"
					>
						<path
							stroke-linecap="round"
							stroke-linejoin="round"
							stroke-width="2"
							d="M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z"
						/>
					</svg>
					<span>{error}</span>
				</div>
			{:else if currentState}
				<div class="stats stats-vertical lg:stats-horizontal shadow w-full">
					<div class="stat">
						<div class="stat-title">State</div>
						<div class="stat-value text-2xl">
							{getStateLabel(currentState.state)}
						</div>
						{#if currentState.state === TrackingState.Commuting && currentState.commuteDirection !== null && currentState.commuteDirection !== undefined}
							<div class="stat-desc">{getCommuteDirectionLabel(currentState.commuteDirection)}</div>
						{/if}
					</div>

					{#if currentState.state !== TrackingState.Idle}
						<div class="stat">
							<div class="stat-title">Duration</div>
							<div class="stat-value text-2xl font-mono">
								{formatElapsedTime(elapsedSeconds)}
							</div>
							<div class="stat-desc">
								Started: {currentState.startedAt
									? formatStartTime(currentState.startedAt)
									: 'N/A'}
							</div>
						</div>
					{/if}
				</div>
			{/if}
		</div>
	</div>

	<!-- Toggle Buttons -->
	<div class="card bg-base-200 shadow-xl mb-6">
		<div class="card-body p-6">
			<h2 class="card-title text-xl mb-2">
				<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-6 h-6">
					<path stroke-linecap="round" stroke-linejoin="round" d="M3.75 13.5l10.5-11.25L12 10.5h8.25L9.75 21.75 12 13.5H3.75z" />
				</svg>
				Quick Actions
			</h2>
			<p class="text-sm text-base-content/70 mb-6">
				Click to toggle state. Long-press or right-click for time offset options.
			</p>

			<div class="flex flex-col sm:flex-row gap-4">
				<!-- Commute Button -->
				<button
					class={getButtonClasses(TrackingState.Commuting)}
					onclick={() => handleButtonClick(TrackingState.Commuting)}
					oncontextmenu={(e) => handleContextMenu(e, TrackingState.Commuting)}
					ontouchstart={() => handleTouchStart(TrackingState.Commuting)}
					ontouchend={handleTouchEnd}
					disabled={loading}
				>
					<svg
						xmlns="http://www.w3.org/2000/svg"
						fill="none"
						viewBox="0 0 24 24"
						stroke-width="1.5"
						stroke="currentColor"
						class="w-8 h-8"
					>
						<path
							stroke-linecap="round"
							stroke-linejoin="round"
							d="M8.25 18.75a1.5 1.5 0 0 1-3 0m3 0a1.5 1.5 0 0 0-3 0m3 0h6m-9 0H3.375a1.125 1.125 0 0 1-1.125-1.125V14.25m17.25 4.5a1.5 1.5 0 0 1-3 0m3 0a1.5 1.5 0 0 0-3 0m3 0h1.125c.621 0 1.129-.504 1.09-1.124a17.902 17.902 0 0 0-3.213-9.193 2.056 2.056 0 0 0-1.58-.86H14.25M16.5 18.75h-2.25m0-11.177v-.958c0-.568-.422-1.048-.987-1.106a48.554 48.554 0 0 0-10.026 0 1.106 1.106 0 0 0-.987 1.106v7.635m12-6.677v6.677m0 4.5v-4.5m0 0h-12"
						/>
					</svg>
					<span>Commute</span>
				</button>

				<!-- Work Button -->
				<button
					class={getButtonClasses(TrackingState.Working)}
					onclick={() => handleButtonClick(TrackingState.Working)}
					oncontextmenu={(e) => handleContextMenu(e, TrackingState.Working)}
					ontouchstart={() => handleTouchStart(TrackingState.Working)}
					ontouchend={handleTouchEnd}
					disabled={loading}
				>
					<svg
						xmlns="http://www.w3.org/2000/svg"
						fill="none"
						viewBox="0 0 24 24"
						stroke-width="1.5"
						stroke="currentColor"
						class="w-8 h-8"
					>
						<path
							stroke-linecap="round"
							stroke-linejoin="round"
							d="M20.25 14.15v4.25c0 1.094-.787 2.036-1.872 2.18-2.087.277-4.216.42-6.378.42s-4.291-.143-6.378-.42c-1.085-.144-1.872-1.086-1.872-2.18v-4.25m16.5 0a2.18 2.18 0 0 0 .75-1.661V8.706c0-1.081-.768-2.015-1.837-2.175a48.114 48.114 0 0 0-3.413-.387m4.5 8.006c-.194.165-.42.295-.673.38A23.978 23.978 0 0 1 12 15.75c-2.648 0-5.195-.429-7.577-1.22a2.016 2.016 0 0 1-.673-.38m0 0A2.18 2.18 0 0 1 3 12.489V8.706c0-1.081.768-2.015 1.837-2.175a48.111 48.111 0 0 1 3.413-.387m7.5 0V5.25A2.25 2.25 0 0 0 13.5 3h-3a2.25 2.25 0 0 0-2.25 2.25v.894m7.5 0a48.667 48.667 0 0 0-7.5 0M12 12.75h.008v.008H12v-.008Z"
						/>
					</svg>
					<span>Work</span>
				</button>

				<!-- Lunch Button -->
				<button
					class={getButtonClasses(TrackingState.Lunch)}
					onclick={() => handleButtonClick(TrackingState.Lunch)}
					oncontextmenu={(e) => handleContextMenu(e, TrackingState.Lunch)}
					ontouchstart={() => handleTouchStart(TrackingState.Lunch)}
					ontouchend={handleTouchEnd}
					disabled={loading}
				>
					<svg
						xmlns="http://www.w3.org/2000/svg"
						fill="none"
						viewBox="0 0 24 24"
						stroke-width="1.5"
						stroke="currentColor"
						class="w-8 h-8"
					>
						<path
							stroke-linecap="round"
							stroke-linejoin="round"
							d="M12 8.25v-1.5m0 1.5c-1.355 0-2.697.056-4.024.166C6.845 8.51 6 9.473 6 10.608v2.513m6-4.871c1.355 0 2.697.056 4.024.166C17.155 8.51 18 9.473 18 10.608v2.513M15 8.25v-1.5m-6 1.5v-1.5m12 9.75-1.5.75a3.354 3.354 0 0 1-3 0 3.354 3.354 0 0 0-3 0 3.354 3.354 0 0 1-3 0 3.354 3.354 0 0 0-3 0 3.354 3.354 0 0 1-3 0L3 16.5m15-3.379a48.474 48.474 0 0 0-6-.371c-2.032 0-4.034.126-6 .371m12 0c.39.049.777.102 1.163.16 1.07.16 1.837 1.094 1.837 2.175v5.169c0 .621-.504 1.125-1.125 1.125H4.125A1.125 1.125 0 0 1 3 20.625v-5.17c0-1.08.768-2.014 1.837-2.174A47.78 47.78 0 0 1 6 13.12M12.265 3.11a.375.375 0 1 1-.53 0L12 2.845l.265.265Zm-3 0a.375.375 0 1 1-.53 0L9 2.845l.265.265Zm6 0a.375.375 0 1 1-.53 0L15 2.845l.265.265Z"
						/>
					</svg>
					<span>Lunch</span>
				</button>
			</div>
		</div>
	</div>

	<!-- Time Offset Menu Modal -->
	{#if showTimeOffsetMenu !== null}
		<div class="modal modal-open">
			<div class="modal-box">
				<h3 class="font-bold text-lg mb-4">
					{getStateLabel(showTimeOffsetMenu)} - Time Offset
				</h3>

				<div class="grid grid-cols-3 gap-2 mb-4">
					<button
						class="btn btn-sm"
						onclick={() => toggleStateWithOffset(showTimeOffsetMenu!, -30)}
						disabled={loading}
					>
						-30m
					</button>
					<button
						class="btn btn-sm"
						onclick={() => toggleStateWithOffset(showTimeOffsetMenu!, -15)}
						disabled={loading}
					>
						-15m
					</button>
					<button
						class="btn btn-sm"
						onclick={() => toggleStateWithOffset(showTimeOffsetMenu!, -5)}
						disabled={loading}
					>
						-5m
					</button>
					<button
						class="btn btn-sm"
						onclick={() => toggleStateWithOffset(showTimeOffsetMenu!, -1)}
						disabled={loading}
					>
						-1m
					</button>
					<button
						class="btn btn-sm btn-primary"
						onclick={() => toggleStateWithOffset(showTimeOffsetMenu!, 0)}
						disabled={loading}
					>
						Now
					</button>
					<button
						class="btn btn-sm"
						onclick={() => toggleStateWithOffset(showTimeOffsetMenu!, 1)}
						disabled={loading}
					>
						+1m
					</button>
					<button
						class="btn btn-sm"
						onclick={() => toggleStateWithOffset(showTimeOffsetMenu!, 5)}
						disabled={loading}
					>
						+5m
					</button>
					<button
						class="btn btn-sm"
						onclick={() => toggleStateWithOffset(showTimeOffsetMenu!, 15)}
						disabled={loading}
					>
						+15m
					</button>
					<button
						class="btn btn-sm"
						onclick={() => toggleStateWithOffset(showTimeOffsetMenu!, 30)}
						disabled={loading}
					>
						+30m
					</button>
				</div>

				<div class="divider">OR</div>

				<div class="form-control">
					<label class="label">
						<span class="label-text">Custom Time or Offset</span>
					</label>
					<div class="join">
						<input
							type="text"
							placeholder="+30m, -2h, or 14:30"
							class="input input-bordered join-item flex-1"
							bind:value={customTime}
							onkeydown={(e) => {
								if (e.key === 'Enter') {
									handleCustomTime(showTimeOffsetMenu!);
								}
							}}
						/>
						<button
							class="btn join-item"
							onclick={() => handleCustomTime(showTimeOffsetMenu!)}
							disabled={loading}
						>
							Set
						</button>
					</div>
					<label class="label">
						<span class="label-text-alt">Examples: +30m, -2h, 14:30</span>
					</label>
				</div>

				<div class="modal-action">
					<button
						class="btn"
						onclick={() => {
							showTimeOffsetMenu = null;
							customTime = '';
						}}
					>
						Cancel
					</button>
				</div>
			</div>
			<div
				class="modal-backdrop"
				onclick={() => {
					showTimeOffsetMenu = null;
					customTime = '';
				}}
			></div>
		</div>
	{/if}

	<!-- Toast Notification -->
	{#if toast}
		<div class="toast toast-top toast-center">
			<div class="alert {toast.type === 'success' ? 'alert-success' : 'alert-error'}">
				<span>{toast.message}</span>
			</div>
		</div>
	{/if}
</div>
