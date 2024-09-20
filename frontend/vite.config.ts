import basicSsl from '@vitejs/plugin-basic-ssl';
import codegen from 'vite-plugin-graphql-codegen';
import { defineConfig } from 'vitest/config';
import { gqlOptions } from './gql-codegen';
// eslint-disable-next-line no-restricted-imports
import precompileIntl from 'svelte-intl-precompile/sveltekit-plugin';
import {type ProxyOptions, searchForWorkspaceRoot} from 'vite';
import { sveltekit } from '@sveltejs/kit/vite';

const inDocker = process.env['DockerDev'] === 'true';
const exposeServer = false;
const ssl = exposeServer && !inDocker;
const lexboxServer: ProxyOptions = {
  target: ssl ? 'https://localhost:7075' : 'http://localhost:5158',
  secure: false,
};

export default defineConfig({
  build: {
    target: 'esnext',
    sourcemap: true
  },
  plugins: [
    {
      resolveId(id: string): string | undefined {
        //workaround for https://github.com/sveltejs/kit/issues/10799
        if (id === 'css-tree') {
          return './node_modules/css-tree/dist/csstree.esm.js';
        }
      }
    },
    codegen(gqlOptions),
    precompileIntl('src/lib/i18n/locales'),
    sveltekit(),
    ssl ? basicSsl() : null, // crypto.subtle is only available on secure connections
  ],
  optimizeDeps: {
  },
  test: {
    include: ['src/**/*.{test,spec}.ts'],
  },
  server: {
    port: 3000,
    host: exposeServer,
    strictPort: true,
    fs: {
      allow: [
        searchForWorkspaceRoot(process.cwd())
      ]
    },
    proxy: inDocker ? undefined : {
      '/v1/traces': 'http://localhost:4318',
      '/api': lexboxServer,
      '/hg': lexboxServer,
      '/.well-known': lexboxServer
    }
  },
});
