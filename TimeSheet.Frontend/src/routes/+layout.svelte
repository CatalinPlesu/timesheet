<script lang="ts">
	import '../app.css';
	import favicon from '$lib/assets/favicon.svg';

	let { children } = $props();
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
				<li><a href="/tracking">Tracking</a></li>
				<li><a href="/entries">Entries</a></li>
				<li><a href="/analytics">Analytics</a></li>
			</ul>
		</div>
	</div>

	<!-- Main Content -->
	<main class="container mx-auto p-4">
		{@render children()}
	</main>
</div>
