const colors = require('tailwindcss/colors');
const plugin = require('tailwindcss/plugin');
// const { createThemes } = require('tw-colors');
const svelteUx = require('svelte-ux/plugins/tailwind.cjs')
const { iconsPlugin, getIconCollections } = require('@egoist/tailwindcss-icons');

module.exports = {
  content: ['./src/**/*.{html,svelte,ts}', './node_modules/svelte-ux/**/*.{svelte,js}'],
  variants: {
    extend: {},
  },
  plugins: [
    iconsPlugin({
      collections: getIconCollections(['mdi']),
    }),
    svelteUx({ colorSpace: 'oklch' }),
  ],
  ux: {
    themeRoot: ':host',
    themes: {
      "light": {
        "color-scheme": "light",
        "--base-text": "#394E6A",
        "primary": "#d1c1d7",
        "secondary": "#f6cbd1",
        "accent": "#b4e9d6",
        "neutral": "#70acc7",
        "surface-100": "oklch(100% 0 0)",
        "surface-200": "#f4f5f6",
        "surface-300": "#d1d5db",
        "surface-content": "#394E6A",
        "warning": "#ff6c00",
        "--rounded-btn": "1.9rem",
        "--tab-radius": "0.7rem",
      },
      "dark": {
        "color-scheme": "dark",
        "--base-text": "#e6e6e6",
        "primary": "oklch(100% 0 0)",
        "secondary": "#152747",
        "accent": "#513448",
        "neutral": "#331800",
        "neutral-content": "#FFE7A3",
        "surface-100": "#151515",
        "surface-200": "#252426",
        "surface-300": "#2e2d2f",
        "surface-content": "#dca54c",
        "info": "#66c6ff",
        "success": "#87d039",
        "warning": "#ff6c00",
        "danger": "#ff6f6f",
      }
    }
  }
};
