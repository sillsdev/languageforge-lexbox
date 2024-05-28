import { defineConfig } from 'vite'
import { svelte } from '@sveltejs/vite-plugin-svelte'

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
      compilerOptions: {
        customElement: true,
      },
      onwarn: (warning, handler) => {
        // we don't have control over these warnings and there are lots
        if (warning.filename?.includes('node_modules/svelte-ux')) return;
        handler(warning);
      },
    })],
    ...(!webComponent ? {
      server: {
        proxy: {
          '/api': {
            target: 'http://localhost:5137',
            secure: false,
            ws: true
          }
        }
      }
    } : {}),
  }
});
