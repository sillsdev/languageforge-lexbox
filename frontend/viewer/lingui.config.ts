import {jstsExtractor, svelteExtractor} from 'svelte-i18n-lingui/extractor';

import {defineConfig} from '@lingui/cli';

export default defineConfig({
  locales: ['en', 'es', 'fr', 'id', 'ko', 'ms', 'sw'],
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
