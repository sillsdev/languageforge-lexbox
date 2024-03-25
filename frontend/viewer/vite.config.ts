import { defineConfig } from 'vite'
import { svelte } from '@sveltejs/vite-plugin-svelte'

// https://vitejs.dev/config/
export default defineConfig(({ mode }) => {
  if (mode === 'web-component') {
    return {
      build: {
        minify: false,
        lib: {
          entry: 'src/web-component.ts',
          formats: ['es'],
        },
        outDir: 'dist-web-component',
      },
      plugins: [svelte({
        compilerOptions: {
          customElement: true,
        },
      })],
    }
  }

  return {
    plugins: [svelte()],
    server: {
      proxy: {
        '/api': {
          target: 'https://localhost:7211',
          secure: false,
          ws: true
        }
      }
    }
  }
});
