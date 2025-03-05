import {jstsExtractor, svelteExtractor} from 'svelte-i18n-lingui/extractor';
import {formatter} from "@lingui/format-json";
import {defineConfig} from "@lingui/cli";

export default defineConfig({
  format: formatter({style: "lingui"}),
  locales: ['en', 'es', 'fr'],
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
