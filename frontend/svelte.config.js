import adapter from '@sveltejs/adapter-node';
import { vitePreprocess } from '@sveltejs/vite-plugin-svelte';

/** @type {import('@sveltejs/kit').Config} */
const config = {
  compilerOptions: {
    warningFilter: (warning) => warning.code != 'element_invalid_self_closing_tag',
    enableSourcemap: true
  },
  onwarn: (warning, handler) => {
    // eslint-plugin-svelte needs its own warning filter, duplicating the one from compilerOptions
    if (warning.code == 'element_invalid_self_closing_tag') return;
    handler(warning);
  },
  kit: {
    alias: {
      '$lib/dotnet-types': 'viewer/src/lib/dotnet-types',
      '$lib/dotnet-types/*': 'viewer/src/lib/dotnet-types/*',
    },
    version: {
      pollInterval: 0,
    },
    adapter: adapter({
      precompress: true
    }),
  },
  preprocess: [
    vitePreprocess({
      script: true,
      postcss: true
    })
  ],
};

export default config;
