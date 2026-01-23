// For more info, see https://github.com/storybookjs/eslint-plugin-storybook#configuration-flat-config-format

import {fileURLToPath} from 'url';
import globals from 'globals';
import js from '@eslint/js';
import path from 'path';
import storybook from "eslint-plugin-storybook";
import stylistic from '@stylistic/eslint-plugin';
import svelte from 'eslint-plugin-svelte';
import svelteParser from 'svelte-eslint-parser';
import tsParser from '@typescript-eslint/parser';
import typescript from 'typescript-eslint';

// mimic CommonJS variables
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

export default [
  {
    ignores: [
      '**/*js',
      '**/generated-types/**',
      '**/vite.config.*',
      '**/.storybook/**',
    ],
  },
  js.configs.recommended,
  {
    plugins: {
      // '@typescript-eslint': typescript, // Included in ...typescript.configs.recommended
      '@stylistic': stylistic,
    },
  },
  ...typescript.configs.recommended,
  ...typescript.configs.recommendedTypeChecked,
  {
    files: ['**/*.svelte'],
    rules: {
      // The Svelte plugin doesn't seem to have typing quite figured out
      '@typescript-eslint/no-redundant-type-constituents': 'off',
      '@typescript-eslint/no-unsafe-argument': 'off',
      '@typescript-eslint/no-unsafe-assignment': 'off',
      '@typescript-eslint/no-unsafe-call': 'off',
      '@typescript-eslint/no-unsafe-member-access': 'off',
      '@typescript-eslint/no-unsafe-return': 'off',
    },
  },
  ...svelte.configs.recommended,
  {
    rules: {
      // https://typescript-eslint.io/rules/
      '@typescript-eslint/naming-convention': [
        'error',
        {
          'selector': 'default',
          'format': ['camelCase'],
          'leadingUnderscore': 'allow',
        },
        {
          'selector': 'function',
          'filter': {'regex': 'GET|POST|PUT|DELETE|PATCH|HEAD|OPTIONS', 'match': true},
          'format': ['UPPER_CASE'],
        },
        {
          'selector': 'default',
          'modifiers': ['const'],
          'format': ['camelCase', 'UPPER_CASE'],
          'leadingUnderscore': 'allow',
        },
        {
          'selector': 'classProperty',
          'modifiers': ['static', 'readonly'],
          'format': ['camelCase', 'UPPER_CASE'],
        },
        {
          'selector': ['typeLike', 'enumMember'],
          'format': ['PascalCase'],
        },
        {
          'selector': 'default',
          'modifiers': ['requiresQuotes'],
          'format': null,
        },
        {
          'selector': 'import',
          'format': ['camelCase', 'PascalCase'],
        },
        {
          'selector': 'variable',
          'modifiers': ['destructured'],
          'filter': {'regex': '^Story$', 'match': true},
          'format': ['PascalCase'],
        }
      ],
      '@stylistic/quotes': ['error', 'single', { 'allowTemplateLiterals': 'always' }],
      '@typescript-eslint/no-unused-vars': [
        'error',
        {
          'argsIgnorePattern': '^_',
          'destructuredArrayIgnorePattern': '^_',
          'caughtErrors': 'all',
          'ignoreRestSiblings': true,
        },
      ],
      '@typescript-eslint/explicit-function-return-type': [
        'off',
        {
          'allowExpressions': true,
        },
      ],
      '@typescript-eslint/consistent-type-imports': ['error', {'fixStyle': 'inline-type-imports'}],
      // https://sveltejs.github.io/eslint-plugin-svelte/rules/
      'svelte/html-quotes': 'error',
      'svelte/no-dom-manipulating': 'warn',
      'svelte/no-reactive-reassign': ['warn', { 'props': false }],
      'svelte/no-store-async': 'error',
      'svelte/require-store-reactive-access': 'error',
      'svelte/mustache-spacing': 'error',
      'svelte/valid-compile' : 'warn',
      'func-style': ['warn', 'declaration'],
      "no-restricted-imports": ["error", {
        "patterns": [{
          "group": ["svelte-intl-precompile"],
          "message": "Use $lib/i18n instead."
        }]
      }]
    },
  },
  {
    languageOptions: {
      parser: tsParser,
      parserOptions: {
        projectService: {
          allowDefaultProject: [
            'lingui.config.ts',
            'tailwind.config.ts',
            'playwright.config.ts',
            'vitest.config.ts',
          ],
        },
        tsconfigRootDir: __dirname,
        extraFileExtensions: ['.svelte'], // Yes, TS-Parser, relax when you're fed svelte files
      },
      globals: {
        ...globals.browser,
        ...globals.node,
      },
    },
  },
  {
    files: ['**/*.svelte'],
    languageOptions: {
      parser: svelteParser,
      parserOptions: {
        parser: tsParser,
      },
      globals: {
        '$$Generic': 'readonly',
      }
    },
  },
  ...storybook.configs["flat/recommended"],
];
