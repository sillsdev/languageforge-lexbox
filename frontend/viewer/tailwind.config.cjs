const colors = require('tailwindcss/colors');
const plugin = require('tailwindcss/plugin');
// const { createThemes } = require('tw-colors');
const svelteUx = require('svelte-ux/plugins/tailwind.cjs')
const { iconsPlugin, getIconCollections } = require('@egoist/tailwindcss-icons');

module.exports = {
  content: ['./src/**/!(WebComponent).{html,svelte,ts}', './node_modules/svelte-ux/**/*.{svelte,js}'],
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
    themeRoot: ':root, :host',
    themes: {
      "light": {
        "color-scheme": "light",
        "--base-text": "#394E6A",
        "primary": "#0050CC",
        "secondary": "#D6E6FF",
        "accent": "#75d7ce",
        "neutral": "#70acc7",
        "surface-100": "oklch(100% 0 0)",
        "surface-200": "#f4f5f6",
        "surface-300": "#d1d5db",
        "surface-content": "#394E6A",
        "info": "#0E94FF",
        "warning": "#ff6c00",
        "--rounded-btn": "1.9rem",
        "--tab-radius": "0.7rem",
      },
      "dark": {
        "color-scheme": "dark",
        "--base-text": "#e6e6e6",
        "primary": "#4882D0",
        "secondary": "#152747",
        "accent": "#1b4d4d",
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
  },
  theme: {
    extend: {
      screens: {
        'max-xs': { 'max': '400px' },
        'max-sm': { 'max': '639px' },
        'max-md': { 'max': '767px' },
        'max-lg': { 'max': '1023px' },
      },
    },
  },
};
