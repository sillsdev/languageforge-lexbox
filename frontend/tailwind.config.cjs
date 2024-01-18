const {iconsPlugin, getIconCollections} = require('@egoist/tailwindcss-icons');
/** @type {import('tailwindcss').Config} */
module.exports = {
    content: ['./src/**/*.{svelte,ts}'],
    plugins: [
        require('@tailwindcss/typography'),
        require('daisyui'),
        iconsPlugin({
            collections: getIconCollections(['mdi']),
        }),
    ],
    daisyui: {
        themes: ['winter', 'business'],
        darkTheme: 'business',
        logs: false
  },
  theme: {
    extend: {
      screens: {
        'sm-only': {'max': '639px'},
      },
    },
  },
};
