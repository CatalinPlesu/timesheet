<script lang="ts">
	import { onMount } from 'svelte';
	import { Chart, registerables } from 'chart.js';
	import { apiClient, type ChartDataDto, type DailyAveragesDto, type CommutePatternsDto, type UserSettingsDto } from '$lib/api';
	import { extractErrorMessage } from '$lib/utils/errorHandling';
	import { formatDuration } from '$lib/utils/timeFormatter';

	// Register Chart.js components
	Chart.register(...registerables);

	// State
	let chartCanvas: HTMLCanvasElement;
	let pieChartCanvas: HTMLCanvasElement;
	let chart: Chart | null = null;
	let pieChart: Chart | null = null;
	let chartData: ChartDataDto | null = null;
	let dailyAverages: DailyAveragesDto | null = null;
	let commuteToWorkPatterns: CommutePatternsDto[] = [];
	let commuteToHomePatterns: CommutePatternsDto[] = [];
	let userSettings: UserSettingsDto | null = null;

	let loading = true;
	let error = '';

	// Form state for date range and grouping
	let startDate = new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0]; // 30 days ago
	let endDate = new Date().toISOString().split('T')[0]; // today
	let groupBy: 'Day' | 'Week' | 'Month' | 'Year' = 'Day';

	const dayNames = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

	async function loadData() {
		loading = true;
		error = '';

		try {
			const start = new Date(startDate);
			const end = new Date(endDate);

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

		// Create new chart with stacked area visualization
		chart = new Chart(chartCanvas, {
			type: 'line',
			data: {
				labels: chartData.labels,
				datasets: [
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
								return label;
							}
						}
					}
				},
				scales: {
					x: {
						grid: {
							display: false
						}
					},
					y: {
						beginAtZero: true,
						title: {
							display: true,
							text: 'Duration'
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

		// Create donut chart
		pieChart = new Chart(pieChartCanvas, {
			type: 'doughnut',
			data: {
				labels: ['Work', 'Commute (To Work)', 'Commute (To Home)', 'Lunch'],
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
					legend: {
						position: 'bottom',
						labels: {
							usePointStyle: true,
							padding: 15
						}
					},
					tooltip: {
						callbacks: {
							label: function (context) {
								const label = context.label || '';
								const value = context.parsed ?? 0;
								return `${label}: ${formatDuration(value)}`;
							}
						}
					}
				}
			}
		});
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

	onMount(() => {
		loadData();
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

	<!-- Filters -->
	<div class="card bg-base-200 shadow-xl mb-6">
		<div class="card-body p-6">
			<h2 class="card-title text-xl mb-4">
				<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-6 h-6">
					<path stroke-linecap="round" stroke-linejoin="round" d="M10.5 6h9.75M10.5 6a1.5 1.5 0 1 1-3 0m3 0a1.5 1.5 0 1 0-3 0M3.75 6H7.5m3 12h9.75m-9.75 0a1.5 1.5 0 0 1-3 0m3 0a1.5 1.5 0 0 0-3 0m-3.75 0H7.5m9-6h3.75m-3.75 0a1.5 1.5 0 0 1-3 0m3 0a1.5 1.5 0 0 0-3 0m-9.75 0h9.75" />
				</svg>
				Filters
			</h2>

			<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
				<div class="form-control">
					<label class="label" for="start-date">
						<span class="label-text font-semibold">Start Date</span>
					</label>
					<input
						id="start-date"
						type="date"
						bind:value={startDate}
						class="input input-bordered w-full"
					/>
				</div>

				<div class="form-control">
					<label class="label" for="end-date">
						<span class="label-text font-semibold">End Date</span>
					</label>
					<input
						id="end-date"
						type="date"
						bind:value={endDate}
						class="input input-bordered w-full"
					/>
				</div>

				<div class="form-control">
					<label class="label" for="group-by">
						<span class="label-text font-semibold">Group By</span>
					</label>
					<select
						id="group-by"
						bind:value={groupBy}
						class="select select-bordered w-full"
					>
						<option value="Day">Day</option>
						<option value="Week">Week</option>
						<option value="Month">Month</option>
						<option value="Year">Year</option>
					</select>
				</div>

				<div class="form-control">
					<div class="label">
						<span class="label-text font-semibold">&nbsp;</span>
					</div>
					<button
						class="btn btn-primary w-full"
						onclick={loadData}
						disabled={loading}
					>
						{#if loading}
							<span class="loading loading-spinner loading-sm"></span>
							Loading...
						{:else}
							<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-5 h-5">
								<path stroke-linecap="round" stroke-linejoin="round" d="M16.023 9.348h4.992v-.001M2.985 19.644v-4.992m0 0h4.992m-4.993 0 3.181 3.183a8.25 8.25 0 0 0 13.803-3.7M4.031 9.865a8.25 8.25 0 0 1 13.803-3.7l3.181 3.182m0-4.991v4.99" />
							</svg>
							Refresh
						{/if}
					</button>
				</div>
			</div>
		</div>
	</div>

	<!-- Charts Row -->
	<div class="grid grid-cols-1 lg:grid-cols-3 gap-6 mb-6">
		<!-- Trend Chart -->
		<div class="card bg-base-200 shadow-xl lg:col-span-2">
			<div class="card-body p-6">
				<h2 class="card-title text-xl mb-4">
					<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-6 h-6">
						<path stroke-linecap="round" stroke-linejoin="round" d="M3 13.125C3 12.504 3.504 12 4.125 12h2.25c.621 0 1.125.504 1.125 1.125v6.75C7.5 20.496 6.996 21 6.375 21h-2.25A1.125 1.125 0 0 1 3 19.875v-6.75ZM9.75 8.625c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125v11.25c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 0 1-1.125-1.125V8.625ZM16.5 4.125c0-.621.504-1.125 1.125-1.125h2.25C20.496 3 21 3.504 21 4.125v15.75c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 0 1-1.125-1.125V4.125Z" />
					</svg>
					Time Trend
				</h2>

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
				<h2 class="card-title text-xl mb-4">
					<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-6 h-6">
						<path stroke-linecap="round" stroke-linejoin="round" d="M10.5 6a7.5 7.5 0 1 0 7.5 7.5h-7.5V6Z" />
						<path stroke-linecap="round" stroke-linejoin="round" d="M13.5 10.5H21A7.5 7.5 0 0 0 13.5 3v7.5Z" />
					</svg>
					Time Distribution
				</h2>

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
					<div class="stat-desc">{dailyAverages.totalWorkDays} work days</div>
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
					<div class="stat-title">Total Duration</div>
					<div class="stat-value text-info">{formatDuration(dailyAverages.averageTotalDurationHours)}</div>
					<div class="stat-desc">Avg per day (incl. all activities)</div>
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
					<h2 class="card-title text-xl mb-4">
						<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-6 h-6">
							<path stroke-linecap="round" stroke-linejoin="round" d="M13.5 4.5 21 12m0 0-7.5 7.5M21 12H3" />
						</svg>
						Commute to Work Patterns
					</h2>

					<div class="overflow-x-auto -mx-6">
						<table class="table table-zebra table-sm">
							<thead>
								<tr>
									<th>Day</th>
									<th>Avg Duration</th>
									<th>Best Time</th>
									<th>Shortest</th>
									<th>Sessions</th>
								</tr>
							</thead>
							<tbody>
								{#each commuteToWorkPatterns as pattern}
									<tr>
										<td class="font-medium">{dayNames[pattern.dayOfWeek]}</td>
										<td>{formatDuration(pattern.averageDurationHours)}</td>
										<td>
											{#if pattern.optimalStartHour !== null && pattern.optimalStartHour !== undefined}
												{pattern.optimalStartHour}:00
											{:else}
												-
											{/if}
										</td>
										<td>
											{#if pattern.shortestDurationHours !== null && pattern.shortestDurationHours !== undefined}
												{formatDuration(pattern.shortestDurationHours)}
											{:else}
												-
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
					<h2 class="card-title text-xl mb-4">
						<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-6 h-6">
							<path stroke-linecap="round" stroke-linejoin="round" d="M10.5 19.5 3 12m0 0 7.5-7.5M3 12h18" />
						</svg>
						Commute to Home Patterns
					</h2>

					<div class="overflow-x-auto -mx-6">
						<table class="table table-zebra table-sm">
							<thead>
								<tr>
									<th>Day</th>
									<th>Avg Duration</th>
									<th>Best Time</th>
									<th>Shortest</th>
									<th>Sessions</th>
								</tr>
							</thead>
							<tbody>
								{#each commuteToHomePatterns as pattern}
									<tr>
										<td class="font-medium">{dayNames[pattern.dayOfWeek]}</td>
										<td>{formatDuration(pattern.averageDurationHours)}</td>
										<td>
											{#if pattern.optimalStartHour !== null && pattern.optimalStartHour !== undefined}
												{pattern.optimalStartHour}:00
											{:else}
												-
											{/if}
										</td>
										<td>
											{#if pattern.shortestDurationHours !== null && pattern.shortestDurationHours !== undefined}
												{formatDuration(pattern.shortestDurationHours)}
											{:else}
												-
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
