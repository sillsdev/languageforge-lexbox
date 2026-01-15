import {jstsExtractor, svelteExtractor} from 'svelte-i18n-lingui/extractor';

import {defineConfig} from '@lingui/cli';
import {locales} from './src/lib/i18n/locales';

const supportedLocales = Object.keys(locales);

export default defineConfig({
  locales: supportedLocales,
  sourceLocale: 'en',
  catalogs: [
    {
      path: 'src/locales/{locale}',
      include: ['src'],
      exclude: ['src/locales']
    },
  ],
  extractors: [jstsExtractor, svelteExtractor],
  formatOptions: {
    lineNumbers: false,
  },
});
