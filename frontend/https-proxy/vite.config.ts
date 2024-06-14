import basicSsl from '@vitejs/plugin-basic-ssl';
import {defineConfig, type ProxyOptions} from 'vite';
import http from 'http';

async function checkTargetAvailability(url: string): Promise<boolean> {
  return new Promise((resolve) => {
    const req = http.get(url, (res) => {
      resolve(!!res.statusCode && res.statusCode < 400);
    });

    req.on('error', () => {
      resolve(false);
    });

    req.end();
  });
}

const targets = ['http://localhost:3000', 'http://localhost'];

const lexboxServer: ProxyOptions = {
  target: targets[0],
  secure: false,
  changeOrigin: false,
  autoRewrite: true,
  protocolRewrite: 'https',
  headers: {
    'x-forwarded-proto': 'https',
  },
  configure: async (proxy, options) => {
    let availableTarget: string | undefined = undefined;

    proxy.on('proxyReq', function () {
      if (!availableTarget) console.warn(`Request before target (${lexboxServer.target}) was confirmed to be available.`);
    });

    while (!availableTarget) {
      for (const target of targets) {
        const isAvailable = await checkTargetAvailability(target);
        if (isAvailable) {
          options.target = availableTarget = target;
          console.log('Will proxy to available target:', target);
          return;
        }
      }
      console.warn('No target available, retrying in 5s');
      await new Promise((resolve) => setTimeout(resolve, 5000));
    }
  },
};

export default defineConfig({
  plugins: [
    basicSsl(),
  ],
  server: {
    port: 3050,
    host: true,
    strictPort: true,
    proxy: {
      '/': lexboxServer,
    }
  },
});
