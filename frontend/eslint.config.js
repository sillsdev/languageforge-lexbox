import { FlatCompat } from '@eslint/eslintrc';
import js from '@eslint/js';
import tsParser from '@typescript-eslint/parser';
import globals from "globals";
import path from "path";
import { fileURLToPath } from "url";

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
    plugins: ['@typescript-eslint'],
    extends: [
      'plugin:@typescript-eslint/recommended',
      'plugin:@typescript-eslint/recommended-requiring-type-checking',
    ],
    ignorePatterns: ['*js'],
  }),
  {
    languageOptions: {
      parser: tsParser,
      parserOptions: {
        project: true,
        tsconfigRootDir: __dirname,
        sourceType: 'module',
      },
      // https://eslint.org/blog/2022/08/new-config-system-part-2/#goodbye-environments%2C-hello-globals
      globals: {
        ...globals.browser,
        ...globals.node,
      }
    },
  },
];
