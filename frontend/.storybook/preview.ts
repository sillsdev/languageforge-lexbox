import '../src/lib/app.postcss';

import type { Preview } from '@storybook/svelte';

globalThis.__sveltekit_dev = {
  env: import.meta.env,
};

const preview: Preview = {
  parameters: {
    actions: { argTypesRegex: '^on[A-Z].*' },
    controls: {
      matchers: {
        color: /(background|color)$/i,
        date: /Date$/i,
      },
    },
  },
};

export default preview;
