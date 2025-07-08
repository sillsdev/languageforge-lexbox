import {jstsExtractor, svelteExtractor} from 'svelte-i18n-lingui/extractor';

import {defineConfig} from '@lingui/cli';
import {formatter} from '@lingui/format-json';

export default defineConfig({
  format: formatter({style: 'lingui'}),
  locales: ['en', 'es', 'fr', 'ko', 'id'],
  sourceLocale: 'en',
  catalogs: [
    {
      path: 'src/locales/{locale}',
      include: ['src'],
      exclude: ['src/locales']
    },
  ],
  extractors: [jstsExtractor, svelteExtractor],
});
