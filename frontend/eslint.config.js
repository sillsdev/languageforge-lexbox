import {fileURLToPath} from 'url';
import globals from 'globals';
import js from '@eslint/js';
import path from 'path';
import svelteParser from 'svelte-eslint-parser';
import tsParser from '@typescript-eslint/parser';
import svelteConfig from './svelte.config.js';
import typescript from 'typescript-eslint';
import stylistic from '@stylistic/eslint-plugin';
import svelte from 'eslint-plugin-svelte';

// mimic CommonJS variables
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

export default [
  {
    ignores: [
      '**/*js',
      'playwright.config.ts',
      '.svelte-kit/**',
      '**/generated/**',
      'viewer/',
      'https-proxy/',
      'platform.bible-extension/'
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
      '@typescript-eslint/no-unsafe-assignment': 'off',
      '@typescript-eslint/no-unsafe-member-access': 'off',
      '@typescript-eslint/no-unsafe-call': 'off',
      '@typescript-eslint/no-unsafe-return': 'off',
      '@typescript-eslint/no-unsafe-argument': 'off',
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
        }
      ],
      '@stylistic/quotes': ['error', 'single', { 'allowTemplateLiterals': true }],
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
        'warn',
        {
          'allowExpressions': true,
          'allowedNames': ['load'],
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
        project: true,
        tsconfigRootDir: __dirname,
        extraFileExtensions: ['.svelte'], // Yes, TS-Parser, relax when you're fed svelte files
        svelteConfig: svelteConfig,
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
  {
    // TEMPORARY IGNORES, to be gotten rid of as soon as possible - 2025-07-24 RM
    files: ['**/*.svelte', '**/*.svelte.ts'],
    rules: {
      'svelte/require-each-key': 'off',
      'svelte/no-useless-mustaches': 'off',
    },
  },
];
