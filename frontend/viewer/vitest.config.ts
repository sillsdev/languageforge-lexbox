import {defineConfig} from 'vitest/config';
import {fileURLToPath} from 'node:url';
import path from 'node:path';
import {storybookTest} from '@storybook/addon-vitest/vitest-plugin';
import {svelte} from '@sveltejs/vite-plugin-svelte';

const dirname =
  typeof __dirname !== 'undefined' ? __dirname : path.dirname(fileURLToPath(import.meta.url));

const browserTestPattern = '**/*.browser.{test,spec}.?(c|m)[jt]s?(x)';
const e2eTestPattern = './tests/**';
const defaultExcludeList = ['**/node_modules/**', '**/dist/**', '**/cypress/**', '**/.{idea,git,cache,output,temp}/**', '**/{karma,rollup,webpack,vite,vitest,jest,ava,babel,nyc,cypress,tsup,build,eslint,prettier}.config.*'];

export default defineConfig({
  test: {
    projects: [
      {
        test: {
          name: 'unit',
          environment: 'node',
          exclude: [browserTestPattern, e2eTestPattern, ...defaultExcludeList],
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
          name: 'browser (non storybook)',
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
