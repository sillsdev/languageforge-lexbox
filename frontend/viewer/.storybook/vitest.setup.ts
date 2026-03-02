import * as previewAnnotations from './preview';

import {afterAll, afterEach, beforeAll} from 'vitest';

import {DemoStoryError} from '../src/stories/demo-story-error';
import {setProjectAnnotations} from '@storybook/svelte-vite';

const annotations = setProjectAnnotations([previewAnnotations]);

// Run Storybook's beforeAll hook
beforeAll(annotations.beforeAll);


const ignoredWarnings = [
  'ownership_invalid_binding',
  'ownership_invalid_mutation',
];

const originalWarn = console.warn.bind(console);
const originalError = console.error.bind(console);

// Collect errors during a single test so we can fail at the end of that test
let collectedErrors: string[] = [];

// Some errors we might want to ignore (add patterns here as needed)
const ignoredErrorPatterns: (RegExp | string)[] = [
  // Example: /ResizeObserver loop limit exceeded/,
];

function matchesIgnoredPattern(message: string): boolean {
  return ignoredErrorPatterns.some((pattern) =>
    typeof pattern === 'string' ? message.includes(pattern) : pattern.test(message),
  );
}

function isDemoStoryError(value: unknown): boolean {
  if (value instanceof DemoStoryError) return true;
  const PromiseRejectionEventCtor = globalThis.PromiseRejectionEvent;
  if (PromiseRejectionEventCtor && value instanceof PromiseRejectionEventCtor) {
    return value.reason instanceof DemoStoryError;
  }
  const ErrorEventCtor = globalThis.ErrorEvent;
  if (ErrorEventCtor && value instanceof ErrorEventCtor) {
    return value.error instanceof DemoStoryError;
  }
  return false;
}

function stringifyErrorArg(arg: unknown): string {
  if (typeof arg === 'string') return arg;
  if (arg instanceof Error) return arg.stack ?? arg.message;
  try {
    return JSON.stringify(arg, null, 2);
  } catch {
    return String(arg);
  }
}

beforeAll(() => {
  console.warn = (...args: unknown[]) => {
    const isIgnoredWarning = args.some(arg => typeof arg === 'string' && ignoredWarnings.some(ignored => arg.includes(ignored)));
    if (isIgnoredWarning) {
      const outOfOurControl = args.some(arg => typeof arg === 'string' && arg.includes('node_modules'));
      if (outOfOurControl) return; // swallow the noise
    }
    originalWarn(...args);
  };
});

// [vibe-coded]: fail tests on error logs and unhandled errors
beforeAll(() => {
  console.error = (...args: unknown[]) => {
    const msg = args.map(stringifyErrorArg).join(' ');
    const isIgnored = args.some(isDemoStoryError) || matchesIgnoredPattern(msg);
    if (!isIgnored) collectedErrors.push(`[error-log] ${msg}`);
    originalError(...args);
  };

  // Listen for uncaught errors & unhandled rejections in the browser environment
  if (typeof window !== 'undefined') {
    window.addEventListener('error', (e) => {
      const msg = e.error?.stack || e.message || String(e);
      const isIgnored = isDemoStoryError(e.error) || matchesIgnoredPattern(msg);
      if (isIgnored) {
        e.preventDefault();
        return;
      }
      collectedErrors.push(`[error] ${msg}`);
    });
    window.addEventListener('unhandledrejection', (e) => {
      const reason = e.reason;
      const msg = typeof reason === 'string' ? reason : (reason?.stack || JSON.stringify(reason));
      const isIgnored = isDemoStoryError(reason) || matchesIgnoredPattern(msg);
      if (isIgnored) {
        e.preventDefault();
        return;
      }
      collectedErrors.push(`[unhandledrejection] ${msg}`);
    });
  }
});

afterEach(() => {
  if (collectedErrors.length) {
    const combined = collectedErrors.join('\n---\n');
    // reset before throwing so subsequent tests start clean
    collectedErrors = [];
    throw new Error('Console/Global errors detected during story render (potentially in previous story):\n' + combined);
  }
});

afterAll(() => {
  console.warn = originalWarn; // Restore the original console.warn
  console.error = originalError; // Restore the original console.error
});
