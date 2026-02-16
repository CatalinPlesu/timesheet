<script lang="ts">
	import { theme, type Theme } from '$lib/stores/theme';

	let currentTheme = $state($theme);
	let isOpen = $state(false);
	let showMoreThemes = $state(false);

	// All additional themes (excluding auto, light, dark)
	const additionalThemes = [
		'cupcake', 'bumblebee', 'emerald', 'corporate', 'fantasy', 'wireframe',
		'cmyk', 'autumn', 'acid', 'lemonade', 'winter', 'nord',
		'synthwave', 'retro', 'cyberpunk', 'valentine', 'halloween', 'garden',
		'forest', 'aqua', 'lofi', 'pastel', 'black', 'luxury', 'dracula',
		'business', 'night', 'coffee', 'dim', 'sunset'
	] as const;

	// Update local state when store changes
	$effect(() => {
		currentTheme = $theme;
	});

	function selectTheme(selectedTheme: Theme) {
		theme.set(selectedTheme);
		isOpen = false;
		showMoreThemes = false;
	}

	function toggleDropdown() {
		isOpen = !isOpen;
		if (!isOpen) {
			showMoreThemes = false;
		}
	}

	function toggleMoreThemes(event: MouseEvent) {
		event.preventDefault();
		event.stopPropagation();
		showMoreThemes = !showMoreThemes;
	}

	// Close dropdown when clicking outside
	function handleClickOutside(event: MouseEvent) {
		const target = event.target as HTMLElement;
		const dropdown = document.getElementById('theme-picker-dropdown');
		if (dropdown && !dropdown.contains(target)) {
			isOpen = false;
			showMoreThemes = false;
		}
	}

	$effect(() => {
		if (isOpen) {
			document.addEventListener('click', handleClickOutside);
			return () => {
				document.removeEventListener('click', handleClickOutside);
			};
		}
	});

	// Get display name for theme
	function getThemeDisplayName(themeValue: Theme): string {
		if (themeValue === 'auto') {
			return 'Auto (System)';
		}
		// Capitalize first letter
		return themeValue.charAt(0).toUpperCase() + themeValue.slice(1);
	}

	// Get icon for current theme
	function getThemeIcon(themeValue: Theme): string {
		if (themeValue === 'auto') {
			return 'M9 12.75 11.25 15 15 9.75m-3-7.036A11.959 11.959 0 0 1 3.598 6 11.99 11.99 0 0 0 3 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285Z';
		}
		const resolved = theme.getResolvedTheme();
		if (resolved === 'light' || ['cupcake', 'bumblebee', 'emerald', 'corporate', 'fantasy', 'wireframe', 'cmyk', 'autumn', 'acid', 'lemonade', 'winter', 'nord'].includes(resolved)) {
			// Sun icon for light themes
			return 'M12 3v2.25m6.364.386-1.591 1.591M21 12h-2.25m-.386 6.364-1.591-1.591M12 18.75V21m-4.773-4.227-1.591 1.591M5.25 12H3m4.227-4.773L5.636 5.636M15.75 12a3.75 3.75 0 1 1-7.5 0 3.75 3.75 0 0 1 7.5 0Z';
		} else {
			// Moon icon for dark themes
			return 'M21.752 15.002A9.72 9.72 0 0 1 18 15.75c-5.385 0-9.75-4.365-9.75-9.75 0-1.33.266-2.597.748-3.752A9.753 9.753 0 0 0 3 11.25C3 16.635 7.365 21 12.75 21a9.753 9.753 0 0 0 9.002-5.998Z';
		}
	}
</script>

<div class="dropdown dropdown-end dropdown-bottom" id="theme-picker-dropdown">
	<button
		onclick={toggleDropdown}
		class="btn btn-ghost btn-sm"
		aria-label="Theme picker"
		type="button"
	>
		<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-5 h-5">
			<path stroke-linecap="round" stroke-linejoin="round" d={getThemeIcon(currentTheme)} />
		</svg>
		<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-4 h-4">
			<path stroke-linecap="round" stroke-linejoin="round" d="m19.5 8.25-7.5 7.5-7.5-7.5" />
		</svg>
	</button>
	{#if isOpen}
		<ul class="dropdown-content menu bg-base-200 rounded-box z-[1] w-64 p-2 shadow-lg max-h-96 overflow-y-auto">
			<li class="menu-title">Theme</li>
			<li>
				<button
					onclick={() => selectTheme('auto')}
					class={currentTheme === 'auto' ? 'active' : ''}
					type="button"
				>
					<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-5 h-5">
						<path stroke-linecap="round" stroke-linejoin="round" d="M9 12.75 11.25 15 15 9.75m-3-7.036A11.959 11.959 0 0 1 3.598 6 11.99 11.99 0 0 0 3 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285Z" />
					</svg>
					Auto (System)
				</button>
			</li>
			<li>
				<button
					onclick={() => selectTheme('light')}
					class={currentTheme === 'light' ? 'active' : ''}
					type="button"
				>
					<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-5 h-5">
						<path stroke-linecap="round" stroke-linejoin="round" d="M12 3v2.25m6.364.386-1.591 1.591M21 12h-2.25m-.386 6.364-1.591-1.591M12 18.75V21m-4.773-4.227-1.591 1.591M5.25 12H3m4.227-4.773L5.636 5.636M15.75 12a3.75 3.75 0 1 1-7.5 0 3.75 3.75 0 0 1 7.5 0Z" />
					</svg>
					Light
				</button>
			</li>
			<li>
				<button
					onclick={() => selectTheme('dark')}
					class={currentTheme === 'dark' ? 'active' : ''}
					type="button"
				>
					<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-5 h-5">
						<path stroke-linecap="round" stroke-linejoin="round" d="M21.752 15.002A9.72 9.72 0 0 1 18 15.75c-5.385 0-9.75-4.365-9.75-9.75 0-1.33.266-2.597.748-3.752A9.753 9.753 0 0 0 3 11.25C3 16.635 7.365 21 12.75 21a9.753 9.753 0 0 0 9.002-5.998Z" />
					</svg>
					Dark
				</button>
			</li>
			<div class="divider my-1"></div>
			<li>
				<button onclick={toggleMoreThemes} class="justify-between" type="button">
					<span>More themes...</span>
					<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-4 h-4 transition-transform {showMoreThemes ? 'rotate-180' : ''}">
						<path stroke-linecap="round" stroke-linejoin="round" d="m19.5 8.25-7.5 7.5-7.5-7.5" />
					</svg>
				</button>
			</li>
			{#if showMoreThemes}
				<div class="divider my-1"></div>
				{#each additionalThemes as themeOption}
					<li>
						<button
							onclick={() => selectTheme(themeOption)}
							class={currentTheme === themeOption ? 'active' : ''}
							type="button"
						>
							<div class="flex items-center gap-2 w-full">
								<div class="flex gap-1">
									<div class="w-2 h-2 rounded-full bg-primary"></div>
									<div class="w-2 h-2 rounded-full bg-secondary"></div>
									<div class="w-2 h-2 rounded-full bg-accent"></div>
								</div>
								<span class="flex-1">{getThemeDisplayName(themeOption)}</span>
							</div>
						</button>
					</li>
				{/each}
			{/if}
		</ul>
	{/if}
</div>
