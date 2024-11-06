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
  {
    ignores: [
      '**/*js',
      '**/generated/**',
      '**/vite.config.*',
    ],
  },
  js.configs.recommended,
  // TypeScript and Svelte plugins don't seem to support the new config format yet
  // So, using backwards compatibility util: https://eslint.org/blog/2022/08/new-config-system-part-2/#backwards-compatibility-utility
  ...compat.config({
    plugins: ['@typescript-eslint'],
    extends: ['plugin:@typescript-eslint/recommended', 'plugin:@typescript-eslint/recommended-requiring-type-checking'],
    overrides: [
      {
        files: ['*.svelte'],
        rules: {
          // The Svelte plugin doesn't seem to have typing quite figured out
          '@typescript-eslint/no-unsafe-assignment': 'off',
          '@typescript-eslint/no-unsafe-member-access': 'off',
          '@typescript-eslint/no-unsafe-call': 'off',
          '@typescript-eslint/no-unsafe-return': 'off',
          '@typescript-eslint/no-unsafe-argument': 'off',
        },
      }
    ]
  }),
  ...compat.config({
    extends: ['plugin:svelte/recommended'],
  }),
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
      '@typescript-eslint/quotes': ['error', 'single', { 'allowTemplateLiterals': true }],
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
];
