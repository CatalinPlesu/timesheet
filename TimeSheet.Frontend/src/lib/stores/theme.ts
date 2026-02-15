import { writable } from 'svelte/store';
import { browser } from '$app/environment';

type Theme = 'light' | 'dark';

const THEME_KEY = 'timesheet-theme';
const DEFAULT_THEME: Theme = 'light';

function createThemeStore() {
	// Get initial theme from localStorage or default
	const initialTheme: Theme = browser
		? (localStorage.getItem(THEME_KEY) as Theme) || DEFAULT_THEME
		: DEFAULT_THEME;

	const { subscribe, set } = writable<Theme>(initialTheme);

	// Apply theme to document
	function applyTheme(theme: Theme) {
		if (browser) {
			document.documentElement.setAttribute('data-theme', theme);
		}
	}

	// Initialize theme on store creation
	if (browser) {
		applyTheme(initialTheme);
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
			if (browser) {
				const currentTheme = localStorage.getItem(THEME_KEY) as Theme || DEFAULT_THEME;
				const newTheme: Theme = currentTheme === 'light' ? 'dark' : 'light';
				localStorage.setItem(THEME_KEY, newTheme);
				applyTheme(newTheme);
				set(newTheme);
			}
		}
	};
}

export const theme = createThemeStore();
