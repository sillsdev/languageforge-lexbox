import {defineConfig} from 'vite';
import {svelte} from '@sveltejs/vite-plugin-svelte';
import {svelteTesting} from '@testing-library/svelte/vite';

// https://vitejs.dev/config/
export default defineConfig(({ mode, command }) => {
  const webComponent = mode === 'web-component';
  return {
    base: !webComponent && command == "build" ? '/_content/FwLiteShared/viewer' : '/',
    build: {
      ...(webComponent ? {
        lib: {
          entry: 'src/web-component.ts',
          formats: ['es'],
        },
        outDir: 'dist-web-component',
      }
        : {
        outDir: '../../backend/FwLite/FwLiteShared/wwwroot/viewer',
        manifest: true,
      }),
      minify: false,
      sourcemap: true,
      chunkSizeWarningLimit: 1000,
      rollupOptions: {
        input: webComponent ? undefined : ['src/main.ts'],
        output: {
          entryFileNames: '[name].js',
          chunkFileNames: '[name].js',
          assetFileNames: '[name][extname]',
          manualChunks: webComponent ? {} : {
            'svelte-ux': ['svelte-ux'],
          },
        },
        onwarn: (warning, handler) => {
          // we don't have control over these warnings
          if (warning.code === 'INVALID_ANNOTATION' && warning.message.includes('node_modules/@microsoft/signalr')) return;
          handler(warning);
        }
      },
    },
    resolve: {
      alias: [{find: "$lib", replacement: "/src/lib"}]
    },
    plugins: [svelte({
      onwarn: (warning, handler) => {
        // we don't have control over these warnings and there are lots
        if (warning.filename?.includes('node_modules/svelte-ux')) return;
        handler(warning);
      },
    }), svelteTesting()],
    server: {
      origin: 'http://localhost:5173',
    },
    test: {
      environment: 'happy-dom',
      setupFiles: ['./vitest-setup.js'],
    },
  }
});
