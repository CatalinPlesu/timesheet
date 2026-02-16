import { writable } from 'svelte/store';
import { browser } from '$app/environment';

// All DaisyUI themes
export const AVAILABLE_THEMES = [
	'light', 'dark', 'cupcake', 'bumblebee', 'emerald', 'corporate',
	'synthwave', 'retro', 'cyberpunk', 'valentine', 'halloween', 'garden',
	'forest', 'aqua', 'lofi', 'pastel', 'fantasy', 'wireframe', 'black',
	'luxury', 'dracula', 'cmyk', 'autumn', 'business', 'acid', 'lemonade',
	'night', 'coffee', 'winter', 'dim', 'nord', 'sunset'
] as const;

export type DaisyTheme = typeof AVAILABLE_THEMES[number];
export type Theme = DaisyTheme | 'auto';

const THEME_KEY = 'timesheet-theme';
const DEFAULT_THEME: Theme = 'auto';

// Detect system theme preference
function getSystemTheme(): DaisyTheme {
	if (browser && window.matchMedia) {
		return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
	}
	return 'light';
}

function createThemeStore() {
	// Get initial theme from localStorage or default
	const storedTheme = browser ? (localStorage.getItem(THEME_KEY) as Theme) : null;
	const initialTheme: Theme = storedTheme || DEFAULT_THEME;

	// Resolve the actual theme to apply
	const resolveTheme = (theme: Theme): DaisyTheme => {
		return theme === 'auto' ? getSystemTheme() : theme;
	};

	const { subscribe, set } = writable<Theme>(initialTheme);

	// Apply theme to document
	function applyTheme(theme: Theme) {
		if (browser) {
			const resolvedTheme = resolveTheme(theme);
			document.documentElement.setAttribute('data-theme', resolvedTheme);
		}
	}

	// Initialize theme on store creation
	if (browser) {
		applyTheme(initialTheme);

		// Listen for system theme changes when in auto mode
		const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
		const handleSystemThemeChange = () => {
			const currentTheme = localStorage.getItem(THEME_KEY) as Theme || DEFAULT_THEME;
			if (currentTheme === 'auto') {
				applyTheme('auto');
			}
		};

		// Use addEventListener for better compatibility
		if (mediaQuery.addEventListener) {
			mediaQuery.addEventListener('change', handleSystemThemeChange);
		} else if (mediaQuery.addListener) {
			// Fallback for older browsers
			mediaQuery.addListener(handleSystemThemeChange);
		}
	}

	return {
		subscribe,
		set: (theme: Theme) => {
			if (browser) {
				localStorage.setItem(THEME_KEY, theme);
				applyTheme(theme);
			}
			set(theme);
		},
		toggle: () => {
			// Toggle between light and dark only (for backwards compatibility)
			if (browser) {
				const currentTheme = localStorage.getItem(THEME_KEY) as Theme || DEFAULT_THEME;
				const resolvedTheme = resolveTheme(currentTheme);
				const newTheme: DaisyTheme = resolvedTheme === 'light' ? 'dark' : 'light';
				localStorage.setItem(THEME_KEY, newTheme);
				applyTheme(newTheme);
				set(newTheme);
			}
		},
		getResolvedTheme: (): DaisyTheme => {
			if (browser) {
				const currentTheme = localStorage.getItem(THEME_KEY) as Theme || DEFAULT_THEME;
				return resolveTheme(currentTheme);
			}
			return 'light';
		}
	};
}

export const theme = createThemeStore();
