const { iconsPlugin, getIconCollections } = require('@egoist/tailwindcss-icons');
const defaultTheme = require('tailwindcss/defaultTheme');

/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./src/**/*.{svelte,ts}'],
  plugins: [
    require('@tailwindcss/typography'),
    require('@tailwindcss/container-queries'),
    require('daisyui'),
    iconsPlugin({
      collections: getIconCollections(['mdi']),
    }),
  ],
  daisyui: {
    themes: [
      {
        'winter': {
          ...require("daisyui/src/theming/themes")["winter"],
          "warning": "#FFBE00", // warning color from corporate, because it has much better contrast
          "primary": "#0058BF", // easier on the eyes
        },
      },
      {
        'business': {
          ...require("daisyui/src/theming/themes")["business"],
          // base colors we had in Daisy 3
          "base-200": "#0F0F0F",
          "base-300": "#000",
        },
      },
    ],
    darkTheme: 'business',
    logs: false,
  },
  theme: {
    screens: {
      'xs': '400px',
      ...defaultTheme.screens,
    },
    extend: {
      screens: {
        'max-xs': { 'max': '400px' },
        'max-sm': { 'max': '639px' },
        'max-md': { 'max': '767px' },
        'admin-tabs': { 'max': '1023px' }, /* aka max-lg, but more explicit */
      },
    },
  },
};
