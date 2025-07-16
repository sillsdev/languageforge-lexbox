import {defineConfig, configDefaults} from 'vitest/config';
import {fileURLToPath} from 'node:url';
import path from 'node:path';
import {storybookTest} from '@storybook/addon-vitest/vitest-plugin';
import {svelte} from '@sveltejs/vite-plugin-svelte';

const dirname =
  typeof __dirname !== 'undefined' ? __dirname : path.dirname(fileURLToPath(import.meta.url));

const browserTestPattern = '**/*.browser.{test,spec}.?(c|m)[jt]s?(x)';
const e2eTestPatterns = ['./tests/**'];

export default defineConfig({
  test: {
    projects: [
      {
        plugins: [
          svelte(),
        ],
        test: {
          name: 'unit',
          // $effect.root requires a dom.
          // We can add a node environment test project later if needed.
          environment:'jsdom',
          exclude: [
            browserTestPattern,
            ...e2eTestPatterns,
            ...configDefaults.exclude
          ]
        },
        resolve: {
          alias: [{find: '$lib', replacement: '/src/lib'}]
        },
      },
      {
        plugins: [
          svelte(),
        ],
        test: {
          name: 'browser',
          browser: {
            enabled: true,
            headless: true,
            provider: 'playwright',
            instances: [
              {browser: 'chromium'},
              {browser: 'firefox'},
            ],
          },
          include: [browserTestPattern],
        },
        resolve: {
          alias: [{find: '$lib', replacement: '/src/lib'}]
        },
      },
      {
        plugins: [
          svelte(),
          // seems to cause this project to only include storybook tests
          storybookTest({
            configDir: path.join(dirname, '.storybook'),
          }),
        ],
        test: {
          name: 'storybook',
          browser: {
            enabled: true,
            headless: true,
            provider: 'playwright',
            instances: [
              {browser: 'chromium'},
              {browser: 'firefox'},
            ],
          },
          setupFiles: ['./.storybook/vitest.setup.ts'],
        },
        resolve: {
          alias: [{find: '$lib', replacement: '/src/lib'}]
        },
      }
    ],
  },
});
