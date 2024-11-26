import adapter from '@sveltejs/adapter-node';
import { vitePreprocess } from '@sveltejs/vite-plugin-svelte';

/** @type {import('@sveltejs/kit').Config} */
const config = {
  compilerOptions: {
    warningFilter: (warning) => warning.code != 'element_invalid_self_closing_tag',
    enableSourcemap: true
  },
  kit: {
    version: {
      pollInterval: 0,
    },
    adapter: adapter({
      precompress: true
    }),
  },
  preprocess: [
    vitePreprocess({
      postcss: true
    })
  ],
};

export default config;
