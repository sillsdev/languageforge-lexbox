import * as previewAnnotations from './preview';

import {afterAll, beforeAll} from 'vitest';
import {setProjectAnnotations} from '@storybook/svelte-vite';

const annotations = setProjectAnnotations([previewAnnotations]);

// Run Storybook's beforeAll hook
beforeAll(annotations.beforeAll);


const ignoredWarnings = [
  'ownership_invalid_binding',
  'ownership_invalid_mutation',
];

const originalWarn = console.warn.bind(console);

beforeAll(() => {
  console.warn = (...args: any[]) => {
    const isIgnoredWarning = args.some(arg => typeof arg === 'string' && ignoredWarnings.some(ignored => arg.includes(ignored)));
    if (isIgnoredWarning) {
      const outOfOurControl = args.some(arg => typeof arg === 'string' && arg.includes('node_modules'));
      if (outOfOurControl) return; // swallow the noise
    }
    originalWarn(...args);
  };
});

afterAll(() => {
  console.warn = originalWarn; // Restore the original console.warn
});
