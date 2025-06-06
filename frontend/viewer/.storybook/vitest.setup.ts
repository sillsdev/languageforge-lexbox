import * as previewAnnotations from './preview';

import {beforeAll} from 'vitest';
import {setProjectAnnotations} from '@storybook/svelte-vite';

const annotations = setProjectAnnotations([previewAnnotations]);

// Run Storybook's beforeAll hook
beforeAll(annotations.beforeAll);
