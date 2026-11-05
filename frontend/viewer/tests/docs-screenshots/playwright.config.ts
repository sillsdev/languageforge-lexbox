import {defineConfig, devices} from '@playwright/test';

// Generates the screenshots embedded in the docs site (docs/site/). These are not tests: nothing is
// asserted beyond "the UI rendered", and no Argos upload happens. Run with `task docs:screenshots`
// (from frontend/viewer) and commit the PNGs it writes under docs/site/static/img/fw-lite/.
//
// Two sources of screenshots:
//  - the running app with the in-browser demo project (/testing/project-view) for whole-screen shots
//  - Storybook stories (iframe.html?id=...) for cropped, single-component shots

// Same override as vite.config.ts, so a worktree on its own port captures its own dev server.
const parsedDevPort = Number(process.env.FwLite__DevAssetsPort);
export const vitePort = Number.isFinite(parsedDevPort) && parsedDevPort > 0 ? parsedDevPort : 5173;
export const storybookPort = 6006;

export default defineConfig({
  testDir: '.',
  testMatch: /.*\.screenshots\.ts/,
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: 0,
  workers: 2,
  outputDir: 'test-results',
  reporter: [['list']],
  use: {
    baseURL: `http://localhost:${vitePort}`,
    storageState: {
      cookies: [],
      origins: [{origin: 'http://localhost', localStorage: [{name: 'isPlaywright', value: 'true'}]}],
    },
    // mode-watcher defaults to "system", so this is what decides light vs dark; individual shots
    // flip it with page.emulateMedia.
    colorScheme: 'light',
  },
  webServer: [
    {
      command: 'pnpm run dev',
      cwd: '../../',
      url: `http://localhost:${vitePort}`,
      reuseExistingServer: true,
    },
    {
      command: `pnpm storybook --ci --no-open -p ${storybookPort}`,
      cwd: '../../',
      url: `http://localhost:${storybookPort}/iframe.html`,
      reuseExistingServer: true,
      timeout: 180_000,
    },
  ],
  projects: [
    {
      name: 'chromium',
      use: {
        ...devices['Desktop Chrome'],
        // Narrow enough that a whole-window shot stays readable in the docs' content column; 2x so
        // it stays sharp when scaled down. After the spread, which sets its own.
        viewport: {width: 1024, height: 640},
        deviceScaleFactor: 2,
        userAgent: 'Playwright Chrome',
      },
    },
  ],
});
