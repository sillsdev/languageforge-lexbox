import {defineConfig, devices} from '@playwright/test';

export default defineConfig({
  testDir: '.',
  testMatch: '**/*.test.ts',
  // E2E tests boot a real FwLiteWeb process and walk a multi-step flow per test.
  timeout: 5 * 60_000,
  expect: {timeout: 30_000},
  fullyParallel: false,
  workers: 1,
  retries: process.env.CI ? 1 : 0,
  forbidOnly: !!process.env.CI,
  outputDir: 'test-results',
  // outputFolder must NOT be inside outputDir; playwright errors otherwise.
  reporter: process.env.CI
    ? [['github'], ['list'], ['junit', {outputFile: 'test-results/e2e-results.xml'}], ['html', {outputFolder: 'e2e-html-report', open: 'never'}]]
    : [['list'], ['html', {outputFolder: 'e2e-html-report', open: 'never'}]],
  use: {
    actionTimeout: 30_000,
    navigationTimeout: 60_000,
    trace: 'on',
    screenshot: 'on',
    video: 'retain-on-failure',
    viewport: {width: 1280, height: 720},
    // Kind cluster ingress uses a snake-oil cert.
    ignoreHTTPSErrors: true,
  },
  projects: [
    {name: 'chromium', use: devices['Desktop Chrome']},
  ],
});
