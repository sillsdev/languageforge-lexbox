import FWLiteDecorator, {initSvelteStoryContext} from './decorators/FWLiteDecorator.svelte';

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
  argTypesEnhancers: [
    (context) => {
      // This is a workaround to make the icon control work with Svelte components
      if (context.argTypes.icon) {
        context.argTypes.icon.control = {type: 'select', labels: {null: 'None'}};
        context.argTypes.icon.options = [null, ...demoIcons];
      }
      return context.argTypes;
    }
  ],
  parameters: {
    controls: {
      matchers: {
        color: /(background|color)$/i,
        date: /Date$/i,
      },
    },
  },
  // @ts-expect-error Bug in Storybook https://github.com/storybookjs/storybook/issues/29951
  decorators: [(_story, storyContext) => {
    // The only way I know how to pass the story context to the decorator
    initSvelteStoryContext(storyContext);
    return FWLiteDecorator;
  }],
};

export default preview;
