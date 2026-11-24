import '../src/app.css';
import FWLiteDecorator, {initSvelteStoryContext} from './decorators/FWLiteDecorator.svelte';

import type {Preview} from '@storybook/svelte-vite';
import {setLanguage} from '$lib/i18n';

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
  // The app does this in main.ts before mounting; without it the language picker shows "Unknown: default".
  loaders: [async () => { await setLanguage('default'); return {}; }],
  parameters: {
    controls: {
      matchers: {
        color: /(background|color)$/i,
        date: /Date$/i,
      },
    },
  },
  decorators: [(_story, storyContext) => {
    // The only way I know how to pass the story context to the decorator
    initSvelteStoryContext(storyContext);
    return {Component: FWLiteDecorator};
  }],
};

export default preview;
