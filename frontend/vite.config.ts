import codegen from 'vite-plugin-graphql-codegen';
import { defineConfig } from 'vitest/config';
import { gqlOptions } from './gql-codegen';
import precompileIntl from 'svelte-intl-precompile/sveltekit-plugin';
import { sveltekit } from '@sveltejs/kit/vite';

export default defineConfig({
  build: {
    target: 'esnext',
    sourcemap: true
  },
  plugins: [
    codegen(gqlOptions),
    precompileIntl('src/lib/i18n/locales'),
    sveltekit(),
  ],
  optimizeDeps: {
  },
  test: {
    include: ['src/**/*.{test,spec}.ts'],
  },
  server: {
    port: 3000,
    strictPort: true,
    proxy: process.env['DockerDev'] ? undefined : {
      '/v1/traces': {
        target: 'http://localhost:4318'
      },
      '/api': {
        target: 'http://localhost:5158'
      },
      '/hg': {
        target: 'http://localhost:5158'
      }
    }
  },
});
