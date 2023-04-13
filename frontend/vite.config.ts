import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig } from 'vitest/config';
import codegen from "vite-plugin-graphql-codegen";
import { gqlOptions } from "./gql-codegen";
import precompileIntl from "svelte-intl-precompile/sveltekit-plugin";

export default defineConfig({
    build: {
		target: 'esnext',
	},
	plugins: [
        codegen(gqlOptions),
        precompileIntl('src/lib/i18n/locales'),
        sveltekit(),
    ],
    optimizeDeps: {
        exclude: ['@urql/svelte']
    },
    test: {
        include: ['src/**/*.{test,spec}.{js,ts}'],
    },
});
