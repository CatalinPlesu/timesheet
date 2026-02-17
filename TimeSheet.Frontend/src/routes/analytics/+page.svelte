<script lang="ts">
	import { onMount } from 'svelte';
	import { Chart, registerables } from 'chart.js';
	import { apiClient, type ChartDataDto, type DailyAveragesDto, type CommutePatternsDto, type UserSettingsDto } from '$lib/api';
	import { extractErrorMessage } from '$lib/utils/errorHandling';
	import { formatDuration } from '$lib/utils/timeFormatter';
	import { auth } from '$lib/stores/auth';
	import { utcToLocal } from '$lib/utils/timeFormatter';

	// Register Chart.js components
	Chart.register(...registerables);

	// State
	let chartCanvas = $state<HTMLCanvasElement>();
	let pieChartCanvas = $state<HTMLCanvasElement>();
	let workHoursChartCanvas = $state<HTMLCanvasElement>();
	let chart = $state<Chart | null>(null);
	let pieChart = $state<Chart | null>(null);
	let workHoursChart = $state<Chart | null>(null);
	let chartData = $state<ChartDataDto | null>(null);
	let dailyAverages = $state<DailyAveragesDto | null>(null);
	let commuteToWorkPatterns = $state<CommutePatternsDto[]>([]);
	let commuteToHomePatterns = $state<CommutePatternsDto[]>([]);
	let userSettings = $state<UserSettingsDto | null>(null);

	let loading = $state(true);
	let error = $state('');
	let workHoursViewMode = $state<'week' | 'month'>('week');

	// Grouping mode — determines how the period is navigated
	let groupBy = $state<'Day' | 'Week' | 'Month' | 'Year'>('Day');

	// Current period — a representative date inside the period being viewed
	let currentPeriod = $state<Date>(new Date());

	const dayNames = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

	// Get UTC offset from auth store
	function getUtcOffsetMinutes(): number {
		return $auth.utcOffsetMinutes ?? 0;
	}

	// Compute start/end dates from currentPeriod + groupBy
	function computeDateRange(): { start: Date; end: Date } {
		const utcOffsetMinutes = getUtcOffsetMinutes();
		const localDate = utcToLocal(currentPeriod, utcOffsetMinutes);

		if (groupBy === 'Day') {
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
			return {
				start: new Date(dayStart.getTime() - utcOffsetMinutes * 60 * 1000),
				end: new Date(dayEnd.getTime() - utcOffsetMinutes * 60 * 1000)
			};
		} else if (groupBy === 'Week') {
			const dayOfWeek = localDate.getUTCDay();
			const daysToMonday = dayOfWeek === 0 ? 6 : dayOfWeek - 1;
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
			return {
				start: new Date(weekStart.getTime() - utcOffsetMinutes * 60 * 1000),
				end: new Date(weekEnd.getTime() - utcOffsetMinutes * 60 * 1000)
			};
		} else if (groupBy === 'Month') {
			const monthStart = new Date(Date.UTC(
				localDate.getUTCFullYear(),
				localDate.getUTCMonth(),
				1,
				0, 0, 0, 0
			));
			const monthEnd = new Date(Date.UTC(
				localDate.getUTCFullYear(),
				localDate.getUTCMonth() + 1,
				0, // last day of month
				23, 59, 59, 999
			));
			return {
				start: new Date(monthStart.getTime() - utcOffsetMinutes * 60 * 1000),
				end: new Date(monthEnd.getTime() - utcOffsetMinutes * 60 * 1000)
			};
		} else {
			// Year
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
			return {
				start: new Date(yearStart.getTime() - utcOffsetMinutes * 60 * 1000),
				end: new Date(yearEnd.getTime() - utcOffsetMinutes * 60 * 1000)
			};
		}
	}

	// Navigate to previous period
	function navigatePrevious() {
		const utcOffsetMinutes = getUtcOffsetMinutes();
		const localDate = utcToLocal(currentPeriod, utcOffsetMinutes);

		if (groupBy === 'Day') {
			localDate.setUTCDate(localDate.getUTCDate() - 1);
		} else if (groupBy === 'Week') {
			localDate.setUTCDate(localDate.getUTCDate() - 7);
		} else if (groupBy === 'Month') {
			localDate.setUTCMonth(localDate.getUTCMonth() - 1);
		} else if (groupBy === 'Year') {
			localDate.setUTCFullYear(localDate.getUTCFullYear() - 1);
		}

		currentPeriod = new Date(localDate.getTime() - utcOffsetMinutes * 60 * 1000);
	}

	// Navigate to next period
	function navigateNext() {
		const utcOffsetMinutes = getUtcOffsetMinutes();
		const localDate = utcToLocal(currentPeriod, utcOffsetMinutes);

		if (groupBy === 'Day') {
			localDate.setUTCDate(localDate.getUTCDate() + 1);
		} else if (groupBy === 'Week') {
			localDate.setUTCDate(localDate.getUTCDate() + 7);
		} else if (groupBy === 'Month') {
			localDate.setUTCMonth(localDate.getUTCMonth() + 1);
		} else if (groupBy === 'Year') {
			localDate.setUTCFullYear(localDate.getUTCFullYear() + 1);
		}

		currentPeriod = new Date(localDate.getTime() - utcOffsetMinutes * 60 * 1000);
	}

	// Jump back to the current period (today / this week / this month / this year)
	function jumpToCurrent() {
		currentPeriod = new Date();
	}

	// Label for the jump-to-current button
	function jumpLabel(): string {
		if (groupBy === 'Day') return 'Today';
		if (groupBy === 'Week') return 'This Week';
		if (groupBy === 'Month') return 'This Month';
		return 'This Year';
	}

	// Human-readable label for the current period
	function getPeriodLabel(): string {
		const utcOffsetMinutes = getUtcOffsetMinutes();
		const localDate = utcToLocal(currentPeriod, utcOffsetMinutes);

		if (groupBy === 'Day') {
			// "Feb 17, 2026"
			return localDate.toLocaleDateString('en-US', {
				timeZone: 'UTC',
				month: 'short',
				day: 'numeric',
				year: 'numeric'
			});
		} else if (groupBy === 'Week') {
			// "Week 8, 2026"
			// ISO week number: week containing Jan 4 is week 1
			const jan4 = new Date(Date.UTC(localDate.getUTCFullYear(), 0, 4));
			const dayOfWeekJan4 = jan4.getUTCDay() === 0 ? 7 : jan4.getUTCDay();
			const week1Start = new Date(jan4.getTime() - (dayOfWeekJan4 - 1) * 86400000);
			const dayOfWeek = localDate.getUTCDay() === 0 ? 7 : localDate.getUTCDay();
			const daysToMonday = dayOfWeek - 1;
			const weekStart = new Date(Date.UTC(
				localDate.getUTCFullYear(),
				localDate.getUTCMonth(),
				localDate.getUTCDate() - daysToMonday
			));
			const weekNum = Math.round((weekStart.getTime() - week1Start.getTime()) / (7 * 86400000)) + 1;
			const year = weekStart.getUTCFullYear();
			return `Week ${weekNum}, ${year}`;
		} else if (groupBy === 'Month') {
			// "February 2026"
			return localDate.toLocaleDateString('en-US', {
				timeZone: 'UTC',
				month: 'long',
				year: 'numeric'
			});
		} else {
			// "2026"
			return localDate.getUTCFullYear().toString();
		}
	}

	async function loadData() {
		loading = true;
		error = '';

		try {
			const { start, end } = computeDateRange();

			// Load all data in parallel
			const [chartResult, averagesResult, toWorkResult, toHomeResult, settingsResult] = await Promise.all([
				apiClient.chartData(start, end, groupBy),
				apiClient.dailyAverages(start, end),
				apiClient.commutePatterns('ToWork', start, end),
				apiClient.commutePatterns('ToHome', start, end),
				apiClient.settings()
			]);

			chartData = chartResult;
			dailyAverages = averagesResult;
			commuteToWorkPatterns = toWorkResult;
			commuteToHomePatterns = toHomeResult;
			userSettings = settingsResult;

			// Update charts
			updateChart();
			updatePieChart();
			updateWorkHoursChart();
		} catch (err) {
			error = extractErrorMessage(err, 'Failed to load analytics data');
			console.error('Analytics error:', err);
		} finally {
			loading = false;
		}
	}

	function updateChart() {
		if (!chartCanvas || !chartData) return;

		// Destroy existing chart
		if (chart) {
			chart.destroy();
		}

		// Build datasets
		const datasets: any[] = [
			{
				label: 'Work',
				data: chartData.workHours,
				backgroundColor: 'rgba(59, 130, 246, 0.3)', // blue-500 with transparency
				borderColor: 'rgba(59, 130, 246, 1)',
				borderWidth: 2,
				fill: true,
				tension: 0.4
			},
			{
				label: 'Commute',
				data: chartData.commuteHours,
				backgroundColor: 'rgba(16, 185, 129, 0.3)', // emerald-500 with transparency
				borderColor: 'rgba(16, 185, 129, 1)',
				borderWidth: 2,
				fill: true,
				tension: 0.4
			},
			{
				label: 'Lunch',
				data: chartData.lunchHours,
				backgroundColor: 'rgba(245, 158, 11, 0.3)', // amber-500 with transparency
				borderColor: 'rgba(245, 158, 11, 1)',
				borderWidth: 2,
				fill: true,
				tension: 0.4
			},
			{
				label: 'Idle',
				data: chartData.idleHours,
				backgroundColor: 'rgba(156, 163, 175, 0.2)', // gray-400 with transparency
				borderColor: 'rgba(156, 163, 175, 0.6)',
				borderWidth: 1,
				borderDash: [5, 5],
				fill: true,
				tension: 0.4
			}
		];

		// Add target line if user settings are available
		if (userSettings?.targetWorkHours) {
			datasets.push({
				type: 'line',
				label: `Target (${formatDuration(userSettings.targetWorkHours)}/day)`,
				data: new Array(chartData.labels.length).fill(userSettings.targetWorkHours),
				borderColor: 'rgba(107, 114, 128, 1)', // gray-500 - neutral color
				borderWidth: 2,
				borderDash: [5, 5],
				pointRadius: 0,
				fill: false,
				tension: 0
			});
		}

		// X-axis label based on grouping mode
		const xAxisLabel = groupBy === 'Day' ? 'Date' : groupBy === 'Week' ? 'Week' : groupBy === 'Month' ? 'Month' : 'Year';

		// Create new chart with stacked area visualization
		chart = new Chart(chartCanvas, {
			type: 'line',
			data: {
				labels: chartData.labels,
				datasets
			},
			options: {
				responsive: true,
				maintainAspectRatio: false,
				interaction: {
					mode: 'index',
					intersect: false
				},
				plugins: {
					title: {
						display: true,
						text: `Time Trend — grouped by ${groupBy}`,
						font: { size: 14, weight: 'bold' },
						padding: { bottom: 12 }
					},
					legend: {
						position: 'top',
						labels: {
							usePointStyle: true,
							padding: 15
						}
					},
					tooltip: {
						callbacks: {
							title: function(items) {
								return items[0]?.label ?? '';
							},
							label: function (context) {
								let label = context.dataset.label || '';
								if (label) {
									label += ': ';
								}
								const value = context.parsed.y ?? 0;
								label += formatDuration(value);
								return label;
							},
							footer: function(items) {
								// Sum all non-target, non-idle values for a "total tracked" footer
								const total = items
									.filter(i => {
										const lbl = i.dataset.label ?? '';
										return !lbl.startsWith('Target') && lbl !== 'Idle';
									})
									.reduce((sum, i) => sum + (i.parsed.y ?? 0), 0);
								if (total > 0) {
									return `Total tracked: ${formatDuration(total)}`;
								}
								return '';
							}
						}
					}
				},
				scales: {
					x: {
						grid: {
							display: false
						},
						title: {
							display: true,
							text: xAxisLabel
						}
					},
					y: {
						beginAtZero: true,
						title: {
							display: true,
							text: 'Duration (h m)'
						},
						ticks: {
							callback: function (value) {
								return formatDuration(value as number);
							}
						}
					}
				}
			}
		});
	}

	function updatePieChart() {
		if (!pieChartCanvas || !dailyAverages) return;

		// Destroy existing chart
		if (pieChart) {
			pieChart.destroy();
		}

		// Calculate total hours for pie chart
		const totalWork = dailyAverages.averageWorkHours * dailyAverages.daysIncluded;
		const totalCommuteToWork = dailyAverages.averageCommuteToWorkHours * dailyAverages.daysIncluded;
		const totalCommuteToHome = dailyAverages.averageCommuteToHomeHours * dailyAverages.daysIncluded;
		const totalLunch = dailyAverages.averageLunchHours * dailyAverages.daysIncluded;
		const grandTotal = totalWork + totalCommuteToWork + totalCommuteToHome + totalLunch;

		// Create donut chart
		pieChart = new Chart(pieChartCanvas, {
			type: 'doughnut',
			data: {
				labels: ['Work', 'Commute to Work', 'Commute to Home', 'Lunch'],
				datasets: [
					{
						data: [totalWork, totalCommuteToWork, totalCommuteToHome, totalLunch],
						backgroundColor: [
							'rgba(59, 130, 246, 0.8)', // blue-500
							'rgba(16, 185, 129, 0.8)', // emerald-500
							'rgba(5, 150, 105, 0.8)', // emerald-600
							'rgba(245, 158, 11, 0.8)' // amber-500
						],
						borderColor: [
							'rgba(59, 130, 246, 1)',
							'rgba(16, 185, 129, 1)',
							'rgba(5, 150, 105, 1)',
							'rgba(245, 158, 11, 1)'
						],
						borderWidth: 2
					}
				]
			},
			options: {
				responsive: true,
				maintainAspectRatio: false,
				plugins: {
					title: {
						display: true,
						text: `Time Distribution (${dailyAverages.daysIncluded} days)`,
						font: { size: 14, weight: 'bold' },
						padding: { bottom: 8 }
					},
					legend: {
						position: 'bottom',
						labels: {
							usePointStyle: true,
							padding: 15,
							generateLabels: function(chart) {
								const data = chart.data;
								if (!data.labels || !data.datasets[0]) return [];
								const dataset = data.datasets[0];
								const values = dataset.data as number[];
								return (data.labels as string[]).map((label, i) => {
									const value = values[i] ?? 0;
									const pct = grandTotal > 0 ? ((value / grandTotal) * 100).toFixed(0) : '0';
									return {
										text: `${label} (${pct}%)`,
										fillStyle: (dataset.backgroundColor as string[])[i],
										strokeStyle: (dataset.borderColor as string[])[i],
										lineWidth: dataset.borderWidth as number,
										hidden: false,
										index: i,
										datasetIndex: 0,
										pointStyle: 'circle' as const
									};
								});
							}
						}
					},
					tooltip: {
						callbacks: {
							label: function (context) {
								const label = context.label || '';
								const value = context.parsed ?? 0;
								const pct = grandTotal > 0 ? ((value / grandTotal) * 100).toFixed(1) : '0.0';
								return `${label}: ${formatDuration(value)} (${pct}%)`;
							},
							footer: function() {
								return `Total: ${formatDuration(grandTotal)}`;
							}
						}
					}
				}
			}
		});
	}


	// Update work hours chart (WakaTime-style)
	function updateWorkHoursChart() {
		if (!workHoursChartCanvas || !chartData || !userSettings?.targetWorkHours) return;

		// Destroy existing chart
		if (workHoursChart) {
			workHoursChart.destroy();
		}

		const targetHours = userSettings.targetWorkHours;
		const labels = chartData.labels;
		const workHours = chartData.workHours;

		// Color bars: red if over target, blue if at/below target
		const backgroundColors = workHours.map(h =>
			h > targetHours ? 'rgba(239, 68, 68, 0.75)' : 'rgba(59, 130, 246, 0.8)'
		);
		const borderColors = workHours.map(h =>
			h > targetHours ? 'rgba(239, 68, 68, 1)' : 'rgba(59, 130, 246, 1)'
		);

		const periodLabel = workHoursViewMode === 'week' ? 'Last 7 Days' : 'This Month';
		const xAxisLabel = workHoursViewMode === 'week' ? 'Date' : 'Day of Month';

		// Create new chart with bars + target annotation line
		workHoursChart = new Chart(workHoursChartCanvas, {
			type: 'bar',
			data: {
				labels,
				datasets: [
					{
						type: 'bar',
						label: 'Work Hours',
						data: workHours,
						backgroundColor: backgroundColors,
						borderColor: borderColors,
						borderWidth: 2,
						borderRadius: 4
					},
					{
						type: 'line',
						label: `Target (${formatDuration(targetHours)}/day)`,
						data: new Array(labels.length).fill(targetHours),
						borderColor: 'rgba(107, 114, 128, 0.9)',
						borderWidth: 2,
						borderDash: [6, 4],
						pointRadius: 0,
						fill: false,
						tension: 0,
						order: 0
					}
				]
			},
			options: {
				responsive: true,
				maintainAspectRatio: false,
				interaction: {
					mode: 'index',
					intersect: false
				},
				plugins: {
					title: {
						display: true,
						text: `Working Hours — ${periodLabel}`,
						font: { size: 14, weight: 'bold' },
						padding: { bottom: 12 }
					},
					legend: {
						position: 'top',
						labels: {
							usePointStyle: true,
							padding: 15
						}
					},
					tooltip: {
						callbacks: {
							label: function (context) {
								let label = context.dataset.label || '';
								if (label) {
									label += ': ';
								}
								const value = context.parsed.y ?? 0;
								label += formatDuration(value);

								// Add difference from target for work hours bar only
								if (context.datasetIndex === 0) {
									const diff = value - targetHours;
									if (diff > 0) {
										label += ` (+${formatDuration(diff)} over target)`;
									} else if (diff < 0) {
										label += ` (${formatDuration(Math.abs(diff))} under target)`;
									} else {
										label += ' (on target)';
									}
								}

								return label;
							}
						}
					}
				},
				scales: {
					x: {
						grid: {
							display: false
						},
						title: {
							display: true,
							text: xAxisLabel
						}
					},
					y: {
						beginAtZero: true,
						title: {
							display: true,
							text: 'Work Duration (h m)'
						},
						ticks: {
							callback: function (value) {
								return formatDuration(value as number);
							}
						}
					}
				}
			}
		});
	}

	// Calculate work hours period totals
	function calculateWorkHoursPeriodTotals(): { totalHours: number; totalOvertime: number; avgPerDay: number; daysAboveTarget: number; daysBelowTarget: number } | null {
		if (!chartData || !userSettings?.targetWorkHours) {
			return null;
		}

		const targetHours = userSettings.targetWorkHours;
		const workHours = chartData.workHours;
		const totalHours = workHours.reduce((sum, hours) => sum + hours, 0);
		const avgPerDay = workHours.length > 0 ? totalHours / workHours.length : 0;
		const totalTarget = targetHours * workHours.length;
		const totalOvertime = totalHours - totalTarget;
		const daysAboveTarget = workHours.filter(hours => hours > targetHours).length;
		const daysBelowTarget = workHours.filter(hours => hours <= targetHours).length;

		return {
			totalHours,
			totalOvertime,
			avgPerDay,
			daysAboveTarget,
			daysBelowTarget
		};
	}

	// Calculate overtime
	function calculateOvertime(): { overtime: number; percentage: number; isOvertime: boolean } | null {
		if (!dailyAverages || !userSettings?.targetWorkHours || dailyAverages.totalWorkDays === 0) {
			return null;
		}

		const targetTotal = userSettings.targetWorkHours * dailyAverages.totalWorkDays;
		const actualTotal = dailyAverages.averageWorkHours * dailyAverages.daysIncluded;
		const overtime = actualTotal - targetTotal;
		const percentage = (actualTotal / targetTotal) * 100;

		return {
			overtime,
			percentage,
			isOvertime: overtime > 0
		};
	}

	// Load work hours data for specific period (week or month)
	async function loadWorkHoursData() {
		loading = true;
		error = '';

		try {
			let start: Date;
			const end = new Date();

			if (workHoursViewMode === 'week') {
				// Get last 7 days
				start = new Date();
				start.setDate(start.getDate() - 6);
			} else {
				// Get current month
				start = new Date(end.getFullYear(), end.getMonth(), 1);
			}

			const result = await apiClient.chartData(start, end, 'Day');
			chartData = result;
			updateWorkHoursChart();
		} catch (err) {
			error = extractErrorMessage(err, 'Failed to load work hours data');
			console.error('Work hours error:', err);
		} finally {
			loading = false;
		}
	}

	let initialLoadComplete = false;

	onMount(() => {
		loadData();
		// Load work hours data separately for the dedicated chart
		loadWorkHoursData();
		// Mark initial load as complete after a short delay to allow charts to render
		setTimeout(() => {
			initialLoadComplete = true;
		}, 100);
	});

	// Auto-refresh when groupBy or currentPeriod changes
	$effect(() => {
		// Read reactive dependencies to track them
		const currentGroupBy = groupBy;
		const period = currentPeriod;

		// Skip the initial load (onMount handles that), and only reload if charts exist
		if (initialLoadComplete) {
			loadData();
		}
	});

	// Update pie chart when data or canvas changes
	$effect(() => {
		if (pieChartCanvas && dailyAverages) {
			updatePieChart();
		}
	});

	// Update main chart when data or canvas changes
	$effect(() => {
		if (chartCanvas && chartData) {
			updateChart();
		}
	});

	// Update work hours chart when data or canvas changes
	$effect(() => {
		if (workHoursChartCanvas && chartData && userSettings?.targetWorkHours) {
			updateWorkHoursChart();
		}
	});
</script>

<div class="max-w-7xl mx-auto px-4">
	<div class="mb-8">
		<h1 class="text-4xl font-bold mb-2">Charts & Analytics</h1>
		<p class="text-base-content/70">Visualize your work patterns and track productivity</p>
	</div>

	{#if error}
		<div class="alert alert-error mb-6">
			<svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6 shrink-0 stroke-current" fill="none" viewBox="0 0 24 24">
				<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z" />
			</svg>
			<span>{error}</span>
		</div>
	{/if}

	<!-- Period Navigation -->
	<div class="card bg-base-200 shadow-xl mb-6">
		<div class="card-body p-6">
			<h2 class="card-title text-xl mb-4">
				<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-6 h-6">
					<path stroke-linecap="round" stroke-linejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 0 1 2.25-2.25h13.5A2.25 2.25 0 0 1 21 7.5v11.25m-18 0A2.25 2.25 0 0 0 5.25 21h13.5A2.25 2.25 0 0 0 21 18.75m-18 0v-7.5A2.25 2.25 0 0 1 5.25 9h13.5A2.25 2.25 0 0 1 21 11.25v7.5" />
				</svg>
				Period
			</h2>

			<div class="flex flex-col gap-4">
				<!-- Group By Select -->
				<div class="form-control w-full sm:w-56">
					<label class="label" for="group-by">
						<span class="label-text font-semibold">Group By</span>
					</label>
					<select
						id="group-by"
						bind:value={groupBy}
						class="select select-bordered w-full"
						onchange={() => { currentPeriod = new Date(); }}
					>
						<option value="Day">Day</option>
						<option value="Week">Week</option>
						<option value="Month">Month</option>
						<option value="Year">Year</option>
					</select>
				</div>

				<!-- Pagination Controls -->
				<div class="flex flex-col sm:flex-row items-stretch sm:items-center gap-3">
					<!-- Prev button -->
					<button
						class="btn btn-outline"
						onclick={navigatePrevious}
						disabled={loading}
						aria-label="Previous period"
					>
						<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-5 h-5">
							<path stroke-linecap="round" stroke-linejoin="round" d="M15.75 19.5 8.25 12l7.5-7.5" />
						</svg>
						Prev
					</button>

					<!-- Current Period Label -->
					<div class="flex-1 text-center">
						<span class="text-lg font-semibold">{getPeriodLabel()}</span>
					</div>

					<!-- Next button -->
					<button
						class="btn btn-outline"
						onclick={navigateNext}
						disabled={loading}
						aria-label="Next period"
					>
						Next
						<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-5 h-5">
							<path stroke-linecap="round" stroke-linejoin="round" d="m8.25 4.5 7.5 7.5-7.5 7.5" />
						</svg>
					</button>

					<!-- Jump to current period -->
					<button
						class="btn btn-ghost btn-sm self-center"
						onclick={jumpToCurrent}
						disabled={loading}
						title="Jump to current period"
					>
						{jumpLabel()}
					</button>
				</div>
			</div>
		</div>
	</div>

	<!-- Work Hours Graph (WakaTime-style) -->
	{#if !loading && chartData && userSettings?.targetWorkHours}
		{@const periodTotals = calculateWorkHoursPeriodTotals()}
		{#if periodTotals}
			<div class="card bg-base-200 shadow-xl mb-6">
				<div class="card-body p-6">
					<div class="flex flex-col md:flex-row md:items-center md:justify-between mb-4">
						<div>
							<h2 class="card-title text-xl mb-1">
								<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-6 h-6">
									<path stroke-linecap="round" stroke-linejoin="round" d="M3 13.125C3 12.504 3.504 12 4.125 12h2.25c.621 0 1.125.504 1.125 1.125v6.75C7.5 20.496 6.996 21 6.375 21h-2.25A1.125 1.125 0 0 1 3 19.875v-6.75ZM9.75 8.625c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125v11.25c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 0 1-1.125-1.125V8.625ZM16.5 4.125c0-.621.504-1.125 1.125-1.125h2.25C20.496 3 21 3.504 21 4.125v15.75c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 0 1-1.125-1.125V4.125Z" />
								</svg>
								Working Hours
							</h2>
							<p class="text-sm text-base-content/60 ml-8">Daily work hours vs. target ({formatDuration(userSettings.targetWorkHours)}/day). Blue = on/under target, red = over target.</p>
						</div>

						<!-- View toggle -->
						<div class="btn-group mt-2 md:mt-0">
							<button
								class="btn btn-sm {workHoursViewMode === 'week' ? 'btn-primary' : 'btn-ghost'}"
								onclick={() => { workHoursViewMode = 'week'; loadWorkHoursData(); }}
							>
								Week
							</button>
							<button
								class="btn btn-sm {workHoursViewMode === 'month' ? 'btn-primary' : 'btn-ghost'}"
								onclick={() => { workHoursViewMode = 'month'; loadWorkHoursData(); }}
							>
								Month
							</button>
						</div>
					</div>

					<!-- Chart -->
					<div class="h-96 w-full mb-4">
						{#if loading}
							<div class="flex items-center justify-center h-full">
								<span class="loading loading-spinner loading-lg"></span>
							</div>
						{:else if chartData}
							<canvas bind:this={workHoursChartCanvas}></canvas>
						{:else}
							<div class="flex flex-col items-center justify-center h-full text-base-content/50">
								<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-16 h-16 mb-4 opacity-50">
									<path stroke-linecap="round" stroke-linejoin="round" d="M3 13.125C3 12.504 3.504 12 4.125 12h2.25c.621 0 1.125.504 1.125 1.125v6.75C7.5 20.496 6.996 21 6.375 21h-2.25A1.125 1.125 0 0 1 3 19.875v-6.75ZM9.75 8.625c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125v11.25c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 0 1-1.125-1.125V8.625ZM16.5 4.125c0-.621.504-1.125 1.125-1.125h2.25C20.496 3 21 3.504 21 4.125v15.75c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 0 1-1.125-1.125V4.125Z" />
								</svg>
								<p>No data available for the selected period</p>
							</div>
						{/if}
					</div>

					<!-- Summary Stats -->
					<div class="grid grid-cols-2 md:grid-cols-5 gap-4">
						<!-- Total Hours -->
						<div class="stats shadow">
							<div class="stat p-4">
								<div class="stat-title text-xs">Total Hours</div>
								<div class="stat-value text-lg">{formatDuration(periodTotals.totalHours)}</div>
								<div class="stat-desc text-xs">{workHoursViewMode === 'week' ? 'This week' : 'This month'}</div>
							</div>
						</div>

						<!-- Average per Day -->
						<div class="stats shadow">
							<div class="stat p-4">
								<div class="stat-title text-xs">Avg per Day</div>
								<div class="stat-value text-lg">{formatDuration(periodTotals.avgPerDay)}</div>
								<div class="stat-desc text-xs">Daily average</div>
							</div>
						</div>

						<!-- Overtime -->
						<div class="stats shadow">
							<div class="stat p-4">
								<div class="stat-title text-xs">{periodTotals.totalOvertime >= 0 ? 'Overtime' : 'Undertime'}</div>
								<div class="stat-value text-lg {periodTotals.totalOvertime >= 0 ? 'text-error' : 'text-success'}">
									{periodTotals.totalOvertime >= 0 ? '+' : ''}{formatDuration(periodTotals.totalOvertime)}
								</div>
								<div class="stat-desc text-xs">vs target</div>
							</div>
						</div>

						<!-- Days Above Target -->
						<div class="stats shadow">
							<div class="stat p-4">
								<div class="stat-title text-xs">Above Target</div>
								<div class="stat-value text-lg text-error">{periodTotals.daysAboveTarget}</div>
								<div class="stat-desc text-xs">days (red bars)</div>
							</div>
						</div>

						<!-- Days Below Target -->
						<div class="stats shadow">
							<div class="stat p-4">
								<div class="stat-title text-xs">At/Below Target</div>
								<div class="stat-value text-lg text-success">{periodTotals.daysBelowTarget}</div>
								<div class="stat-desc text-xs">days (blue bars)</div>
							</div>
						</div>
					</div>
				</div>
			</div>
		{/if}
	{/if}

	<!-- Charts Row -->
	<div class="grid grid-cols-1 lg:grid-cols-3 gap-6 mb-6">
		<!-- Trend Chart -->
		<div class="card bg-base-200 shadow-xl lg:col-span-2">
			<div class="card-body p-6">
				<h2 class="card-title text-xl mb-1">
					<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-6 h-6">
						<path stroke-linecap="round" stroke-linejoin="round" d="M3 13.125C3 12.504 3.504 12 4.125 12h2.25c.621 0 1.125.504 1.125 1.125v6.75C7.5 20.496 6.996 21 6.375 21h-2.25A1.125 1.125 0 0 1 3 19.875v-6.75ZM9.75 8.625c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125v11.25c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 0 1-1.125-1.125V8.625ZM16.5 4.125c0-.621.504-1.125 1.125-1.125h2.25C20.496 3 21 3.504 21 4.125v15.75c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 0 1-1.125-1.125V4.125Z" />
					</svg>
					Time Trend
				</h2>
				<p class="text-sm text-base-content/60 mb-4 ml-8">Stacked area chart of daily activity. Hover for exact durations and totals.</p>

				<div class="h-96 w-full">
					{#if loading}
						<div class="flex items-center justify-center h-full">
							<span class="loading loading-spinner loading-lg"></span>
						</div>
					{:else if chartData}
						<canvas bind:this={chartCanvas}></canvas>
					{:else}
						<div class="flex flex-col items-center justify-center h-full text-base-content/50">
							<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-16 h-16 mb-4 opacity-50">
								<path stroke-linecap="round" stroke-linejoin="round" d="M3 13.125C3 12.504 3.504 12 4.125 12h2.25c.621 0 1.125.504 1.125 1.125v6.75C7.5 20.496 6.996 21 6.375 21h-2.25A1.125 1.125 0 0 1 3 19.875v-6.75ZM9.75 8.625c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125v11.25c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 0 1-1.125-1.125V8.625ZM16.5 4.125c0-.621.504-1.125 1.125-1.125h2.25C20.496 3 21 3.504 21 4.125v15.75c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 0 1-1.125-1.125V4.125Z" />
							</svg>
							<p>No data available for the selected period</p>
						</div>
					{/if}
				</div>
			</div>
		</div>

		<!-- Pie Chart -->
		<div class="card bg-base-200 shadow-xl">
			<div class="card-body p-6">
				<h2 class="card-title text-xl mb-1">
					<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-6 h-6">
						<path stroke-linecap="round" stroke-linejoin="round" d="M10.5 6a7.5 7.5 0 1 0 7.5 7.5h-7.5V6Z" />
						<path stroke-linecap="round" stroke-linejoin="round" d="M13.5 10.5H21A7.5 7.5 0 0 0 13.5 3v7.5Z" />
					</svg>
					Time Distribution
				</h2>
				<p class="text-sm text-base-content/60 mb-4 ml-8">Share of total time per activity. Legend shows % of total.</p>

				<div class="h-96 w-full">
					{#if loading}
						<div class="flex items-center justify-center h-full">
							<span class="loading loading-spinner loading-lg"></span>
						</div>
					{:else if dailyAverages && dailyAverages.daysIncluded > 0}
						<canvas bind:this={pieChartCanvas}></canvas>
					{:else}
						<div class="flex flex-col items-center justify-center h-full text-base-content/50">
							<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-16 h-16 mb-4 opacity-50">
								<path stroke-linecap="round" stroke-linejoin="round" d="M10.5 6a7.5 7.5 0 1 0 7.5 7.5h-7.5V6Z" />
								<path stroke-linecap="round" stroke-linejoin="round" d="M13.5 10.5H21A7.5 7.5 0 0 0 13.5 3v7.5Z" />
							</svg>
							<p>No data available</p>
						</div>
					{/if}
				</div>
			</div>
		</div>
	</div>

	<!-- Overtime Tracking -->
	{#if !loading && dailyAverages && userSettings?.targetWorkHours}
		{@const overtimeData = calculateOvertime()}
		{#if overtimeData}
			<div class="card bg-base-200 shadow-xl mb-6">
				<div class="card-body p-6">
					<h2 class="card-title text-xl mb-4">
						<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-6 h-6">
							<path stroke-linecap="round" stroke-linejoin="round" d="M3.75 3v11.25A2.25 2.25 0 0 0 6 16.5h2.25M3.75 3h-1.5m1.5 0h16.5m0 0h1.5m-1.5 0v11.25A2.25 2.25 0 0 1 18 16.5h-2.25m-7.5 0h7.5m-7.5 0-1 3m8.5-3 1 3m0 0 .5 1.5m-.5-1.5h-9.5m0 0-.5 1.5M9 11.25v1.5M12 9v3.75m3-6v6" />
						</svg>
						Overtime Tracking
					</h2>

					<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
						<!-- Target Work Hours -->
						<div class="stats shadow">
							<div class="stat">
								<div class="stat-title">Target per Day</div>
								<div class="stat-value text-sm">{formatDuration(userSettings.targetWorkHours)}</div>
								<div class="stat-desc">Expected work hours</div>
							</div>
						</div>

						<!-- Total Target -->
						<div class="stats shadow">
							<div class="stat">
								<div class="stat-title">Total Target</div>
								<div class="stat-value text-sm">
									{formatDuration(userSettings.targetWorkHours * dailyAverages.totalWorkDays)}
								</div>
								<div class="stat-desc">{dailyAverages.totalWorkDays} work days</div>
							</div>
						</div>

						<!-- Actual Work Hours -->
						<div class="stats shadow">
							<div class="stat">
								<div class="stat-title">Actual Hours</div>
								<div class="stat-value text-sm">
									{formatDuration(dailyAverages.averageWorkHours * dailyAverages.daysIncluded)}
								</div>
								<div class="stat-desc">{dailyAverages.daysIncluded} days tracked</div>
							</div>
						</div>

						<!-- Overtime/Undertime -->
						<div class="stats shadow">
							<div class="stat">
								<div class="stat-title">
									{overtimeData.isOvertime ? 'Overtime' : 'Undertime'}
								</div>
								<div class="stat-value text-sm {overtimeData.isOvertime ? 'text-warning' : 'text-info'}">
									{overtimeData.isOvertime ? '+' : ''}{formatDuration(Math.abs(overtimeData.overtime))}
								</div>
								<div class="stat-desc">
									{overtimeData.percentage.toFixed(1)}% of target
								</div>
							</div>
						</div>
					</div>

					<!-- Progress Bar -->
					<div class="mt-4">
						<div class="flex justify-between text-sm mb-2">
							<span>Progress toward target</span>
							<span class="font-semibold">{overtimeData.percentage.toFixed(1)}%</span>
						</div>
						<progress
							class="progress {overtimeData.isOvertime ? 'progress-warning' : 'progress-primary'} w-full"
							value={overtimeData.percentage}
							max="100"
						></progress>
					</div>
				</div>
			</div>
		{/if}
	{/if}

	<!-- Daily Averages -->
	{#if dailyAverages}
		<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-6">
			<div class="stats shadow">
				<div class="stat">
					<div class="stat-figure text-primary">
						<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-8 h-8">
							<path stroke-linecap="round" stroke-linejoin="round" d="M20.25 14.15v4.25c0 1.094-.787 2.036-1.872 2.18-2.087.277-4.216.42-6.378.42s-4.291-.143-6.378-.42c-1.085-.144-1.872-1.086-1.872-2.18v-4.25m16.5 0a2.18 2.18 0 0 0 .75-1.661V8.706c0-1.081-.768-2.015-1.837-2.175a48.114 48.114 0 0 0-3.413-.387m4.5 8.006c-.194.165-.42.295-.673.38A23.978 23.978 0 0 1 12 15.75c-2.648 0-5.195-.429-7.577-1.22a2.016 2.016 0 0 1-.673-.38m0 0A2.18 2.18 0 0 1 3 12.489V8.706c0-1.081.768-2.015 1.837-2.175a48.111 48.111 0 0 1 3.413-.387m7.5 0V5.25A2.25 2.25 0 0 0 13.5 3h-3a2.25 2.25 0 0 0-2.25 2.25v.894m7.5 0a48.667 48.667 0 0 0-7.5 0M12 12.75h.008v.008H12v-.008Z" />
						</svg>
					</div>
					<div class="stat-title">Avg Work Hours</div>
					<div class="stat-value text-primary">{formatDuration(dailyAverages.averageWorkHours)}</div>
					<div class="stat-desc">{dailyAverages.totalWorkDays} work days in range</div>
				</div>
			</div>

			<div class="stats shadow">
				<div class="stat">
					<div class="stat-figure text-success">
						<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-8 h-8">
							<path stroke-linecap="round" stroke-linejoin="round" d="M8.25 18.75a1.5 1.5 0 0 1-3 0m3 0a1.5 1.5 0 0 0-3 0m3 0h6m-9 0H3.375a1.125 1.125 0 0 1-1.125-1.125V14.25m17.25 4.5a1.5 1.5 0 0 1-3 0m3 0a1.5 1.5 0 0 0-3 0m3 0h1.125c.621 0 1.129-.504 1.09-1.124a17.902 17.902 0 0 0-3.213-9.193 2.056 2.056 0 0 0-1.58-.86H14.25M16.5 18.75h-2.25m0-11.177v-.958c0-.568-.422-1.048-.987-1.106a48.554 48.554 0 0 0-10.026 0 1.106 1.106 0 0 0-.987 1.106v7.635m12-6.677v6.677m0 4.5v-4.5m0 0h-12" />
						</svg>
					</div>
					<div class="stat-title">Avg Commute (Total)</div>
					<div class="stat-value text-success">
						{formatDuration(dailyAverages.averageCommuteToWorkHours + dailyAverages.averageCommuteToHomeHours)}
					</div>
					<div class="stat-desc">
						To work: {formatDuration(dailyAverages.averageCommuteToWorkHours)} /
						To home: {formatDuration(dailyAverages.averageCommuteToHomeHours)}
					</div>
				</div>
			</div>

			<div class="stats shadow">
				<div class="stat">
					<div class="stat-figure text-warning">
						<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-8 h-8">
							<path stroke-linecap="round" stroke-linejoin="round" d="M12 6v6h4.5m4.5 0a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
						</svg>
					</div>
					<div class="stat-title">Avg Lunch</div>
					<div class="stat-value text-warning">{formatDuration(dailyAverages.averageLunchHours)}</div>
					<div class="stat-desc">Per work day</div>
				</div>
			</div>

			<div class="stats shadow">
				<div class="stat">
					<div class="stat-figure text-info">
						<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-8 h-8">
							<path stroke-linecap="round" stroke-linejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 0 1 2.25-2.25h13.5A2.25 2.25 0 0 1 21 7.5v11.25m-18 0A2.25 2.25 0 0 0 5.25 21h13.5A2.25 2.25 0 0 0 21 18.75m-18 0v-7.5A2.25 2.25 0 0 1 5.25 9h13.5A2.25 2.25 0 0 1 21 11.25v7.5" />
						</svg>
					</div>
					<div class="stat-title">Avg Total Duration</div>
					<div class="stat-value text-info">{formatDuration(dailyAverages.averageTotalDurationHours)}</div>
					<div class="stat-desc">All activities per day</div>
				</div>
			</div>
		</div>
	{/if}

	<!-- Commute Patterns -->
	{#if commuteToWorkPatterns.length > 0 || commuteToHomePatterns.length > 0}
		<div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
			<!-- Commute to Work -->
			<div class="card bg-base-200 shadow-xl">
				<div class="card-body p-6">
					<h2 class="card-title text-xl mb-1">
						<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-6 h-6">
							<path stroke-linecap="round" stroke-linejoin="round" d="M13.5 4.5 21 12m0 0-7.5 7.5M21 12H3" />
						</svg>
						Commute to Work Patterns
					</h2>
					<p class="text-sm text-base-content/60 mb-3 ml-8">By day of week. "Best Departure" = hour with shortest commute time.</p>

					<div class="overflow-x-auto -mx-6">
						<table class="table table-zebra table-sm">
							<thead>
								<tr>
									<th>Day</th>
									<th>Avg Duration</th>
									<th>Best Departure</th>
									<th>Shortest Trip</th>
									<th>Trips</th>
								</tr>
							</thead>
							<tbody>
								{#each commuteToWorkPatterns as pattern}
									<tr>
										<td class="font-medium">{dayNames[pattern.dayOfWeek]}</td>
										<td>{formatDuration(pattern.averageDurationHours)}</td>
										<td>
											{#if pattern.optimalStartHour !== null && pattern.optimalStartHour !== undefined}
												{pattern.optimalStartHour.toString().padStart(2, '0')}:00
											{:else}
												—
											{/if}
										</td>
										<td>
											{#if pattern.shortestDurationHours !== null && pattern.shortestDurationHours !== undefined}
												{formatDuration(pattern.shortestDurationHours)}
											{:else}
												—
											{/if}
										</td>
										<td>{pattern.sessionCount}</td>
									</tr>
								{/each}
							</tbody>
						</table>
					</div>
				</div>
			</div>

			<!-- Commute to Home -->
			<div class="card bg-base-200 shadow-xl">
				<div class="card-body p-6">
					<h2 class="card-title text-xl mb-1">
						<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-6 h-6">
							<path stroke-linecap="round" stroke-linejoin="round" d="M10.5 19.5 3 12m0 0 7.5-7.5M3 12h18" />
						</svg>
						Commute to Home Patterns
					</h2>
					<p class="text-sm text-base-content/60 mb-3 ml-8">By day of week. "Best Departure" = hour with shortest commute time.</p>

					<div class="overflow-x-auto -mx-6">
						<table class="table table-zebra table-sm">
							<thead>
								<tr>
									<th>Day</th>
									<th>Avg Duration</th>
									<th>Best Departure</th>
									<th>Shortest Trip</th>
									<th>Trips</th>
								</tr>
							</thead>
							<tbody>
								{#each commuteToHomePatterns as pattern}
									<tr>
										<td class="font-medium">{dayNames[pattern.dayOfWeek]}</td>
										<td>{formatDuration(pattern.averageDurationHours)}</td>
										<td>
											{#if pattern.optimalStartHour !== null && pattern.optimalStartHour !== undefined}
												{pattern.optimalStartHour.toString().padStart(2, '0')}:00
											{:else}
												—
											{/if}
										</td>
										<td>
											{#if pattern.shortestDurationHours !== null && pattern.shortestDurationHours !== undefined}
												{formatDuration(pattern.shortestDurationHours)}
											{:else}
												—
											{/if}
										</td>
										<td>{pattern.sessionCount}</td>
									</tr>
								{/each}
							</tbody>
						</table>
					</div>
				</div>
			</div>
		</div>
	{/if}
</div>
