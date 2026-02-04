import {fileURLToPath} from 'url';
import path from 'path';
import {sveltePreprocess} from 'svelte-preprocess';
import {vitePreprocess} from '@sveltejs/vite-plugin-svelte';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const typescriptConfig = path.join(__dirname, 'tsconfig.json');

export default {
  compilerOptions: {
    warningFilter: (warning) => warning.code !== 'element_invalid_self_closing_tag' && warning.code !== 'custom_element_props_identifier',
    customElement: true,//required for storybook tests
  },
  // Consult https://svelte.dev/docs#compile-time-svelte-preprocess
  // for more information about preprocessors
  preprocess: [vitePreprocess(), sveltePreprocess({
    typescript: {
      tsconfigFile: typescriptConfig,
    },
  })],
};
