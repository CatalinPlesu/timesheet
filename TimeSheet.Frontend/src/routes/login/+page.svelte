<script lang="ts">
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth';
	import { apiClient, LoginRequest } from '$lib/api';
	import { scheduleTokenRefresh } from '$lib/utils/tokenRefresh';
	import { onMount } from 'svelte';

	let mnemonic = $state('');
	let isLoading = $state(false);
	let errorMessage = $state('');
	let showPassword = $state(false);

	// Redirect if already authenticated
	onMount(() => {
		const unsubscribe = auth.subscribe(state => {
			if (state.isAuthenticated) {
				goto('/tracking');
			}
		});
		return unsubscribe;
	});

	async function handleLogin() {
		// Reset error
		errorMessage = '';

		// Validate mnemonic
		const trimmedMnemonic = mnemonic.trim();
		if (!trimmedMnemonic) {
			errorMessage = 'Please enter your mnemonic phrase';
			return;
		}

		// Check if it's roughly 24 words (simple validation)
		const words = trimmedMnemonic.split(/\s+/).filter(w => w.length > 0);
		if (words.length !== 24) {
			errorMessage = `Mnemonic must be exactly 24 words (found ${words.length} words)`;
			return;
		}

		isLoading = true;

		try {
			// Normalize the mnemonic (join the filtered words with single spaces)
			const normalizedMnemonic = words.join(' ');
			const request = new LoginRequest({ mnemonic: normalizedMnemonic });
			const response = await apiClient.login(request);

			// Store tokens and UTC offset in auth store
			auth.login(response.accessToken, response.refreshToken, response.expiresAt, response.utcOffsetMinutes);

			// Schedule token refresh
			scheduleTokenRefresh();

			// Redirect to tracking page
			await goto('/tracking');
		} catch (error: any) {
			console.error('Login failed:', error);

			// Parse error message
			if (error.response) {
				try {
					const errorData = JSON.parse(error.response);
					errorMessage = errorData.detail || errorData.title || 'Invalid mnemonic phrase';
				} catch {
					errorMessage = 'Invalid mnemonic phrase';
				}
			} else if (error.message) {
				errorMessage = error.message;
			} else {
				errorMessage = 'Login failed. Please check your mnemonic and try again.';
			}
		} finally {
			isLoading = false;
		}
	}

	function handleKeyDown(event: KeyboardEvent) {
		if (event.key === 'Enter' && !isLoading) {
			handleLogin();
		}
	}
</script>

<div class="flex items-center justify-center min-h-[calc(100vh-8rem)]">
	<div class="card w-full max-w-md bg-base-200 shadow-xl">
		<div class="card-body">
			<!-- Header -->
			<div class="flex items-center justify-center mb-4">
				<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-12 h-12 text-primary">
					<path stroke-linecap="round" stroke-linejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 1 0-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 0 0 2.25-2.25v-6.75a2.25 2.25 0 0 0-2.25-2.25H6.75a2.25 2.25 0 0 0-2.25 2.25v6.75a2.25 2.25 0 0 0 2.25 2.25Z" />
				</svg>
			</div>
			<h2 class="card-title text-2xl justify-center mb-2">Welcome to TimeSheet</h2>
			<p class="text-center text-sm text-base-content/70 mb-6">
				Enter your 24-word mnemonic from Telegram
			</p>

			<!-- Error Alert -->
			{#if errorMessage}
				<div role="alert" class="alert alert-error mb-4">
					<svg xmlns="http://www.w3.org/2000/svg" class="stroke-current shrink-0 h-6 w-6" fill="none" viewBox="0 0 24 24">
						<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z" />
					</svg>
					<span>{errorMessage}</span>
				</div>
			{/if}

			<!-- Login Form -->
			<div class="form-control">
				<label class="label" for="mnemonic">
					<span class="label-text">Mnemonic Phrase</span>
				</label>
				<div class="relative">
					<input
						id="mnemonic"
						type={showPassword ? 'text' : 'password'}
						class="input input-bordered w-full font-mono text-sm pr-12"
						placeholder="word1 word2 word3 ... word24"
						bind:value={mnemonic}
						onkeydown={handleKeyDown}
						disabled={isLoading}
					/>
					<button
						type="button"
						class="absolute right-3 top-1/2 -translate-y-1/2 btn btn-ghost btn-sm btn-circle"
						onclick={() => showPassword = !showPassword}
						tabindex="-1"
					>
						{#if showPassword}
							<!-- Eye Slash Icon (hide) -->
							<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-5 h-5">
								<path stroke-linecap="round" stroke-linejoin="round" d="M3.98 8.223A10.477 10.477 0 0 0 1.934 12C3.226 16.338 7.244 19.5 12 19.5c.993 0 1.953-.138 2.863-.395M6.228 6.228A10.451 10.451 0 0 1 12 4.5c4.756 0 8.773 3.162 10.065 7.498a10.522 10.522 0 0 1-4.293 5.774M6.228 6.228 3 3m3.228 3.228 3.65 3.65m7.894 7.894L21 21m-3.228-3.228-3.65-3.65m0 0a3 3 0 1 0-4.243-4.243m4.242 4.242L9.88 9.88" />
							</svg>
						{:else}
							<!-- Eye Icon (show) -->
							<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-5 h-5">
								<path stroke-linecap="round" stroke-linejoin="round" d="M2.036 12.322a1.012 1.012 0 0 1 0-.639C3.423 7.51 7.36 4.5 12 4.5c4.638 0 8.573 3.007 9.963 7.178.07.207.07.431 0 .639C20.577 16.49 16.64 19.5 12 19.5c-4.638 0-8.573-3.007-9.963-7.178Z" />
								<path stroke-linecap="round" stroke-linejoin="round" d="M15 12a3 3 0 1 1-6 0 3 3 0 0 1 6 0Z" />
							</svg>
						{/if}
					</button>
				</div>
				<div class="label">
					<span class="label-text-alt text-base-content/60">
						Paste the 24-word phrase from the Telegram /login command
					</span>
				</div>
			</div>

			<!-- Submit Button -->
			<div class="card-actions justify-end mt-4">
				<button
					class="btn btn-primary w-full"
					onclick={handleLogin}
					disabled={isLoading}
				>
					{#if isLoading}
						<span class="loading loading-spinner"></span>
						Authenticating...
					{:else}
						<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-5 h-5">
							<path stroke-linecap="round" stroke-linejoin="round" d="M13.5 4.5 21 12m0 0-7.5 7.5M21 12H3" />
						</svg>
						Sign In
					{/if}
				</button>
			</div>

			<!-- Help Text -->
			<div class="divider text-xs">Need help?</div>
			<p class="text-center text-xs text-base-content/60">
				Get your login mnemonic by typing <code class="kbd kbd-sm">/login</code> in the Telegram bot
			</p>
		</div>
	</div>
</div>
