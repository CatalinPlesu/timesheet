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
	<div class="navbar bg-base-200 shadow-lg">
		<div class="flex-1">
			<a href="/" class="btn btn-ghost text-xl">TimeSheet</a>
		</div>
		<div class="flex-none">
			<ul class="menu menu-horizontal px-1">
				{#if showNav}
					<li><a href="/tracking">Tracking</a></li>
					<li><a href="/entries">Entries</a></li>
					<li><a href="/analytics">Analytics</a></li>
					<li>
						<button onclick={handleLogout}>
							<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-5 h-5">
								<path stroke-linecap="round" stroke-linejoin="round" d="M15.75 9V5.25A2.25 2.25 0 0 0 13.5 3h-6a2.25 2.25 0 0 0-2.25 2.25v13.5A2.25 2.25 0 0 0 7.5 21h6a2.25 2.25 0 0 0 2.25-2.25V15m3 0 3-3m0 0-3-3m3 3H9" />
							</svg>
							Logout
						</button>
					</li>
				{:else}
					<li><a href="/about">About</a></li>
				{/if}
			</ul>
		</div>
	</div>

	<!-- Main Content -->
	<main class="container mx-auto p-4">
		{@render children()}
	</main>
</div>
