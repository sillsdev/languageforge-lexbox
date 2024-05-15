import basicSsl from '@vitejs/plugin-basic-ssl';
import codegen from 'vite-plugin-graphql-codegen';
import { defineConfig } from 'vitest/config';
import { gqlOptions } from './gql-codegen';
// eslint-disable-next-line no-restricted-imports
import precompileIntl from 'svelte-intl-precompile/sveltekit-plugin';
import {type ProxyOptions, searchForWorkspaceRoot} from 'vite';
import { sveltekit } from '@sveltejs/kit/vite';





const exposeServer = false;
const lexboxServer: ProxyOptions = {
  target: 'https://localhost:7075',
  secure: false
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
    basicSsl()
  ],
  optimizeDeps: {
  },
  test: {
    include: ['src/**/*.{test,spec}.ts'],
  },
  server: {
    port: 3000,
    host: exposeServer,
    https: {

    },
    strictPort: true,
    fs: {
      allow: [
        searchForWorkspaceRoot(process.cwd())
      ]
    },
    proxy: process.env['DockerDev'] ? undefined : {
      '/v1/traces': lexboxServer,
      '/api': lexboxServer,
      '/hg': lexboxServer,
      '/.well-known': lexboxServer
    }
  },
});
