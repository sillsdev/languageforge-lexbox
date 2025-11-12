import basicSsl from '@vitejs/plugin-basic-ssl';
import {defineConfig} from 'vite';
import {lingui} from '@lingui/vite-plugin';
import {svelte} from '@sveltejs/vite-plugin-svelte';
import webfontDownload from 'vite-plugin-webfont-dl';

const ssl = false;

// https://vitejs.dev/config/
export default defineConfig(({ mode, command }) => {
  return {
    base: command == "build" ? '/_content/FwLiteShared/viewer' : '/',
    build: {
      outDir: '../../backend/FwLite/FwLiteShared/wwwroot/viewer',
      emptyOutDir: true,
      manifest: true,
      minify: false,
      sourcemap: true,
      chunkSizeWarningLimit: 1000,
      rollupOptions: {
        input: ['src/main.ts'],
        output: {
          entryFileNames: '[name].js',
          chunkFileNames: '[name].js',
          assetFileNames: '[name][extname]',
        },
        onwarn: (warning, handler) => {
          // we don't have control over these warnings
          if (warning.code === 'INVALID_ANNOTATION' && warning.message.includes('node_modules/@microsoft/signalr')) return;
          handler(warning);
        }
      },
    },
    resolve: {
      alias: [
        {find: "$lib", replacement: "/src/lib"},
        {find: "$project", replacement: "/src/project"}
      ]
    },
    plugins: [
      svelte(),
      lingui(),
      webfontDownload([],
      {
        assetsSubfolder: 'fonts',
        minifyCss: false
      }),
      ssl ? basicSsl() : null, // crypto.subtle is only available on secure connections
    ],
    server: {
      origin: 'http://localhost:5173',
      host: true,
      allowedHosts: true,
      cors: true,
    },
  }
});
