import basicSsl from '@vitejs/plugin-basic-ssl';
import {defineConfig} from 'vite';
import {lingui} from '@lingui/vite-plugin';
import {svelte} from '@sveltejs/vite-plugin-svelte';
import tailwindcss from '@tailwindcss/vite';
import webfontDownload from 'vite-plugin-webfont-dl';

const ssl = false;

// Override the dev-server port (and the origin it advertises) via FW_LITE_DEV_PORT so multiple
// worktrees can each run their own viewer; the .NET host loads assets cross-origin, so origin must track the port.
const parsedDevPort = Number(process.env.FW_LITE_DEV_PORT);
const devPortFromEnv = Number.isFinite(parsedDevPort) && parsedDevPort > 0;
const devPort = devPortFromEnv ? parsedDevPort : 5173;

// https://vitejs.dev/config/
export default defineConfig(({command}) => ({
  base: command === 'build' ? '/_content/FwLiteShared/viewer' : '/',
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
      tailwindcss(),
      svelte(),
      lingui(),
      webfontDownload([], {
        assetsSubfolder: 'fonts',
        minifyCss: false
      }),
      ssl ? basicSsl() : null, // crypto.subtle is only available on secure connections
    ],
  server: {
      port: devPort,
      strictPort: devPortFromEnv,
      origin: `http://localhost:${devPort}`,
      host: true,
      allowedHosts: true,
      cors: true,
    },
}));
