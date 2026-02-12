import { defineConfig } from '@lingui/cli';

export default defineConfig({
  // Same locales as the FwLite viewer
  locales: ['en', 'es', 'fr'],
  sourceLocale: 'en',
  catalogs: [
    {
      path: 'src/locales/{locale}',
      include: ['src'],
      exclude: ['src/locales'],
    },
  ],
  formatOptions: {
    lineNumbers: false,
  },
});
