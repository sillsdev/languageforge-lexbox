import { FlatCompat } from '@eslint/eslintrc';
import { fileURLToPath } from 'url';
import globals from 'globals';
import js from '@eslint/js';
import path from 'path';
import svelteParser from 'svelte-eslint-parser';
import tsParser from '@typescript-eslint/parser';

// mimic CommonJS variables
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const compat = new FlatCompat({
  baseDirectory: __dirname
});

export default [
  js.configs.recommended,
  // TypeScript libs don't seem to support the new config format yet
  // compat: https://eslint.org/blog/2022/08/new-config-system-part-2/#backwards-compatibility-utility
  ...compat.config({
    extends: [
      'plugin:@typescript-eslint/recommended',
      // 'plugin:@typescript-eslint/recommended-requiring-type-checking'], // doesn't seem to play well with svelte
    ],
    ignorePatterns: ['*js'],
  }),
  ...compat.config({
    extends: ['plugin:svelte/recommended'],
  }),
  {
    languageOptions: {
      parser: tsParser,
      parserOptions: {
        project: true,
        tsconfigRootDir: __dirname,
        sourceType: 'module',
        extraFileExtensions: ['.svelte'],
      },
    },
  },
  {
    files: ['**/*.svelte'],
    languageOptions: {
      parser: svelteParser,
      parserOptions: {
        parser: '@typescript-eslint/parser',
      }
    },
  },
  {
    languageOptions: {
      // https://eslint.org/blog/2022/08/new-config-system-part-2/#goodbye-environments%2C-hello-globals
      globals: {
        ...globals.browser,
        ...globals.node,
      }
    }
  }
];
