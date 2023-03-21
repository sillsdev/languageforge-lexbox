import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig } from 'vitest/config';
import codegen from "vite-plugin-graphql-codegen";
import { gqlOptions } from "./gql-codegen";
import precompileIntl from "svelte-intl-precompile/sveltekit-plugin";

export default defineConfig({
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
    server: {
        proxy: {
            '/api': {
                target: 'http://localhost:5158'
            },
            //hg web will try to load static files from /static, but they need to be routed to the backend so it can proxy to hg web
            '/static': {
                target: 'http://localhost:5158',
                bypass: (req, res) => {
                    const referer = req.headers.referer ?? '';
                    if (referer.includes('/api/hg-view/') || referer.includes('/static/hgk/css/icofont.min.css')) {
                        return undefined;
                    }
                    return req.url;
                }
            }
        }
    },
});
