import '../src/app.postcss';

import type {Preview} from '@storybook/svelte-vite';

const demoIcons = [
  // realistic
  'i-mdi-auto-fix',
  'i-mdi-cloud',
  'i-mdi-pencil-outline',
  'i-mdi-close',
  // fun
  'i-mdi-party-popper',
  'i-mdi-cake-variant',
  'i-mdi-guitar-electric',
  'i-mdi-pine-tree',
  'i-mdi-water-polo',
];

const preview: Preview = {
  argTypes: {
    icon: {
      control: { type: 'select', labels: { null: 'None' } },
      options: [null, ...demoIcons],
    },
  },
  parameters: {
    controls: {
      matchers: {
        color: /(background|color)$/i,
        date: /Date$/i,
      },
    },
  },
};

export default preview;
