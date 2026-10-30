import {defineConfig, devices} from '@playwright/test';

// Generates the screenshots embedded in the docs site (docs/). These are not tests: nothing is
// asserted beyond "the UI rendered", and no Argos upload happens. Run with `task docs:screenshots`
// (from frontend/viewer) and commit the PNGs it writes under docs/static/img/fw-lite/.
//
// Two sources of screenshots:
//  - the running app with the in-browser demo project (/testing/project-view) for whole-screen shots
//  - Storybook stories (iframe.html?id=...) for cropped, single-component shots
export const vitePort = 5173;
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
    viewport: {width: 1280, height: 720},
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
        userAgent: 'Playwright Chrome',
        // Lets an environment with a preinstalled Chromium skip `playwright install`.
        launchOptions: process.env.CHROMIUM_PATH ? {executablePath: process.env.CHROMIUM_PATH} : undefined,
      },
    },
  ],
});
