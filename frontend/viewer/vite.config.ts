import {defineConfig} from 'vite';
import {svelte} from '@sveltejs/vite-plugin-svelte';
import {svelteTesting} from '@testing-library/svelte/vite';

// https://vitejs.dev/config/
export default defineConfig(({ mode }) => {
  const webComponent = mode === 'web-component';
  return {
    build: {
      ...(webComponent ? {
        lib: {
          entry: 'src/web-component.ts',
          formats: ['es'],
        },
        outDir: 'dist-web-component',
      } : {}),
      chunkSizeWarningLimit: 1000,
      rollupOptions: {
        output: {
          manualChunks: {
            'svelte-ux': ['svelte-ux'],
          }
        },
        onwarn: (warning, handler) => {
          // we don't have control over these warnings
          if (warning.code === 'INVALID_ANNOTATION' && warning.message.includes('node_modules/@microsoft/signalr')) return;
          handler(warning);
        }
      },
    },
    plugins: [svelte({
      onwarn: (warning, handler) => {
        // we don't have control over these warnings and there are lots
        if (warning.filename?.includes('node_modules/svelte-ux')) return;
        handler(warning);
      },
    }), svelteTesting()],
    ...(!webComponent ? {
      server: {
        open: 'http://localhost:5173/testing/project-view',
        proxy: {
          '/api': {
            target: 'http://localhost:5137',
            secure: false,
            ws: true
          }
        }
      }
    } : {}),
    test: {
      environment: 'happy-dom',
      setupFiles: ['./vitest-setup.js'],
    },
  }
});
