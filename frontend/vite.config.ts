import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig } from 'vitest/config';
import codegen from "vite-plugin-graphql-codegen";
import { gqlOptions } from "./gql-codegen";

export default defineConfig({
    plugins: [
        codegen(gqlOptions),
        sveltekit(),
    ],
    optimizeDeps: {
        exclude: ['@urql/svelte']
    },
    test: {
        include: ['src/**/*.{test,spec}.{js,ts}'],
    },
    server: {
        proxy: {
            '/api': {
                target: 'http://localhost:5158'
            }
        }
    },
});
