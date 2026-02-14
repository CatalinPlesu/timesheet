<script lang="ts">
	import { auth } from '$lib/stores/auth';
	import { goto } from '$app/navigation';
	import { onMount } from 'svelte';

	onMount(() => {
		const unsubscribe = auth.subscribe(state => {
			if (state.isAuthenticated) {
				goto('/tracking');
			} else {
				goto('/login');
			}
		});
		return unsubscribe;
	});
</script>

<div class="flex items-center justify-center min-h-[calc(100vh-8rem)]">
	<div class="loading loading-spinner loading-lg"></div>
</div>
