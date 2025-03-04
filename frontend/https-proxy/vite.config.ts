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

    await new Promise<void>(resolve => {
      const initActiveTarget = setInterval(async () => {
        const bestTarget = await getBestAvailableTarget();
        if (bestTarget) {
          options.target = availableTarget = bestTarget;
          console.log('Will proxy to best available target:', bestTarget);
          clearInterval(initActiveTarget);
          monitorActiveTarget(options);
          resolve();
        }
      }, 5_000);
    })
  },
};

function monitorActiveTarget(options) {
  setInterval(async () => {
    const bestTarget = await getBestAvailableTarget();
    if (bestTarget && options.target !== bestTarget) {
      console.log('Switching to new best available proxy target:', bestTarget);
      options.target = bestTarget;
    }
  }, 30_000);
}

async function getBestAvailableTarget() {
  for (const target of targets) {
    if (await checkTargetAvailability(target)) return target;
  }
}

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
