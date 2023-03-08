/** @type {import('tailwindcss').Config} */
module.exports = {
	content: ['./src/**/*.{svelte,ts}'],
	plugins: [
		require('@tailwindcss/typography'),
		require('daisyui'),
	],
}
