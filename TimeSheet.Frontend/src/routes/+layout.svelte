<script lang="ts">
	import '../app.css';
	import favicon from '$lib/assets/favicon.svg';
	import { auth } from '$lib/stores/auth';
	import { page } from '$app/stores';
	import { goto } from '$app/navigation';
	import { onMount } from 'svelte';
	import { cancelTokenRefresh } from '$lib/utils/tokenRefresh';

	let { children } = $props();

	// Public routes that don't require authentication
	const publicRoutes = ['/login', '/about'];

	// Check authentication and redirect if needed
	onMount(() => {
		// Register service worker for PWA support
		if ('serviceWorker' in navigator) {
			navigator.serviceWorker.register('/sw.js').then(
				(registration) => {
					console.log('Service Worker registered with scope:', registration.scope);

					// Handle updates
					registration.addEventListener('updatefound', () => {
						const newWorker = registration.installing;
						if (newWorker) {
							newWorker.addEventListener('statechange', () => {
								if (newWorker.state === 'installed' && navigator.serviceWorker.controller) {
									// New service worker available, prompt user to refresh
									console.log('New version available! Please refresh the page.');
								}
							});
						}
					});
				},
				(error) => {
					console.error('Service Worker registration failed:', error);
				}
			);
		}

		const unsubscribe = auth.subscribe(state => {
			const currentPath = window.location.pathname;
			const isPublicRoute = publicRoutes.some(route => currentPath === route);

			if (!state.isAuthenticated && !isPublicRoute) {
				goto('/login');
			}
		});

		return unsubscribe;
	});

	function handleLogout() {
		cancelTokenRefresh();
		auth.logout();
		goto('/login');
	}

	// Reactive check for showing navigation
	let showNav = $derived($auth.isAuthenticated);
</script>

<svelte:head>
	<link rel="icon" href={favicon} />
	<title>TimeSheet</title>
</svelte:head>

<div class="min-h-screen bg-base-100">
	<!-- Navigation -->
	<div class="navbar bg-base-200 shadow-lg sticky top-0 z-50">
		<div class="flex-1">
			<a href="/" class="btn btn-ghost text-xl font-bold">
				<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-6 h-6">
					<path stroke-linecap="round" stroke-linejoin="round" d="M12 6v6h4.5m4.5 0a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
				</svg>
				TimeSheet
			</a>
		</div>
		<div class="flex-none">
			<ul class="menu menu-horizontal px-1 gap-1">
				{#if showNav}
					<li>
						<a href="/tracking" class="btn btn-ghost btn-sm">
							<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-5 h-5">
								<path stroke-linecap="round" stroke-linejoin="round" d="M5.25 5.653c0-.856.917-1.398 1.667-.986l11.54 6.347a1.125 1.125 0 0 1 0 1.972l-11.54 6.347a1.125 1.125 0 0 1-1.667-.986V5.653Z" />
							</svg>
							<span class="hidden sm:inline">Tracking</span>
						</a>
					</li>
					<li>
						<a href="/entries" class="btn btn-ghost btn-sm">
							<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-5 h-5">
								<path stroke-linecap="round" stroke-linejoin="round" d="M3.375 19.5h17.25m-17.25 0a1.125 1.125 0 01-1.125-1.125M3.375 19.5h7.5c.621 0 1.125-.504 1.125-1.125m-9.75 0V5.625m0 12.75v-1.5c0-.621.504-1.125 1.125-1.125m18.375 2.625V5.625m0 12.75c0 .621-.504 1.125-1.125 1.125m1.125-1.125v-1.5c0-.621-.504-1.125-1.125-1.125m0 3.75h-7.5A1.125 1.125 0 0112 18.375m9.75-12.75c0-.621-.504-1.125-1.125-1.125H3.375c-.621 0-1.125.504-1.125 1.125m19.5 0v1.5c0 .621-.504 1.125-1.125 1.125M2.25 5.625v1.5c0 .621.504 1.125 1.125 1.125m0 0h17.25m-17.25 0h7.5c.621 0 1.125.504 1.125 1.125M3.375 8.25c-.621 0-1.125.504-1.125 1.125v1.5c0 .621.504 1.125 1.125 1.125m17.25-3.75h-7.5c-.621 0-1.125.504-1.125 1.125m8.625-1.125c.621 0 1.125.504 1.125 1.125v1.5c0 .621-.504 1.125-1.125 1.125m-17.25 0h7.5m-7.5 0c-.621 0-1.125.504-1.125 1.125v1.5c0 .621.504 1.125 1.125 1.125M12 10.875v-1.5m0 1.5c0 .621-.504 1.125-1.125 1.125M12 10.875c0 .621.504 1.125 1.125 1.125m-2.25 0c.621 0 1.125.504 1.125 1.125M13.125 12h7.5m-7.5 0c-.621 0-1.125.504-1.125 1.125M20.625 12c.621 0 1.125.504 1.125 1.125v1.5c0 .621-.504 1.125-1.125 1.125m-17.25 0h7.5M12 14.625v-1.5m0 1.5c0 .621-.504 1.125-1.125 1.125M12 14.625c0 .621.504 1.125 1.125 1.125m-2.25 0c.621 0 1.125.504 1.125 1.125m0 1.5v-1.5m0 0c0-.621.504-1.125 1.125-1.125m0 0h7.5" />
							</svg>
							<span class="hidden sm:inline">Entries</span>
						</a>
					</li>
					<li>
						<a href="/analytics" class="btn btn-ghost btn-sm">
							<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-5 h-5">
								<path stroke-linecap="round" stroke-linejoin="round" d="M3 13.125C3 12.504 3.504 12 4.125 12h2.25c.621 0 1.125.504 1.125 1.125v6.75C7.5 20.496 6.996 21 6.375 21h-2.25A1.125 1.125 0 0 1 3 19.875v-6.75ZM9.75 8.625c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125v11.25c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 0 1-1.125-1.125V8.625ZM16.5 4.125c0-.621.504-1.125 1.125-1.125h2.25C20.496 3 21 3.504 21 4.125v15.75c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 0 1-1.125-1.125V4.125Z" />
							</svg>
							<span class="hidden sm:inline">Analytics</span>
						</a>
					</li>
					<li>
						<button onclick={handleLogout} class="btn btn-ghost btn-sm">
							<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-5 h-5">
								<path stroke-linecap="round" stroke-linejoin="round" d="M15.75 9V5.25A2.25 2.25 0 0 0 13.5 3h-6a2.25 2.25 0 0 0-2.25 2.25v13.5A2.25 2.25 0 0 0 7.5 21h6a2.25 2.25 0 0 0 2.25-2.25V15m3 0 3-3m0 0-3-3m3 3H9" />
							</svg>
							<span class="hidden sm:inline">Logout</span>
						</button>
					</li>
				{:else}
					<li><a href="/about" class="btn btn-ghost btn-sm">About</a></li>
				{/if}
			</ul>
		</div>
	</div>

	<!-- Main Content -->
	<main class="container mx-auto py-8">
		{@render children()}
	</main>

	<!-- Footer -->
	<footer class="footer footer-center p-4 bg-base-200 text-base-content/70 mt-auto">
		<aside>
			<p class="text-sm">TimeSheet - Personal work hour tracking © 2026</p>
		</aside>
	</footer>
</div>
