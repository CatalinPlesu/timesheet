/** @type {import('tailwindcss').Config} */
export default {
  content: ['./src/**/*.{html,js,svelte,ts}'],
  theme: {
    extend: {},
  },
  plugins: [require('daisyui')],
  daisyui: {
    themes: [
      {
        timesheet: {
          primary: '#f59e0b', // amber-500 (Phoenix-like warm accent)
          secondary: '#8b5cf6', // violet-500
          accent: '#06b6d4', // cyan-500
          neutral: '#1f2937', // gray-800
          'base-100': '#ffffff', // white background
          'base-200': '#f3f4f6', // gray-100
          'base-300': '#e5e7eb', // gray-200
          info: '#3b82f6', // blue-500
          success: '#10b981', // emerald-500
          warning: '#f59e0b', // amber-500
          error: '#ef4444', // red-500
        },
      },
    ],
  },
}

