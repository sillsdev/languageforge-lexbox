import {MOBILE_BREAKPOINT} from './src/css-breakpoints';
import {getIconCollections} from '@egoist/tailwindcss-icons';
import {iconsPlugin} from '@egoist/tailwindcss-icons';
import typography from '@tailwindcss/typography';

export default {
  content: [
    './src/**/*.{html,svelte,ts}',
    './.storybook/**/*.{html,svelte,ts}',
    //exclude icons.d.ts, because it contains all the icon classes which would cause them all to be included in the bundle
    '!./src/lib/icon-class.ts'
  ],
  plugins: [
    iconsPlugin({
      // Root source: https://github.com/Templarian/MaterialDesign
      // Our source (that pulls from ☝️): https://www.npmjs.com/package/@iconify-json/mdi
      // Search showing aliases and version (of root source) icons were introduced: https://pictogrammers.com/library/mdi/
      collections: getIconCollections(['mdi']),
    }),
    typography,
  ],
  theme: {
    extend: {
      screens: {
        'md': `${MOBILE_BREAKPOINT}px`,
        'max-xs': {'max': '400px'},
        'max-sm': {'max': '639px'},
        'max-md': {'max': '767px'},

        // Breakpoints for the entry form aka editor
        'xs-form': {'max': '800px'},
        'sm-form': {'max': '1279px'},
        'lg-form': {'min': '1280px'},

        // Breakpoints for the project view layout
        'sm-view': {'max': '800px'},
        'lg-view': '801px',
      },
    },
  },
};
