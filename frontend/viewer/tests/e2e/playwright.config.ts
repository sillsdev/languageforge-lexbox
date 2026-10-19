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
    ? [
      ['github'],
      ['list'],
      ['junit', {outputFile: 'test-results/e2e-results.xml'}],
      ['html', {outputFolder: 'e2e-html-report', open: 'never'}],
      // Distinct buildName: under the shared default, Argos compares this suite against the ui suite's baseline and marks its screenshots "removed".
      ['@argos-ci/playwright/reporter', {uploadToArgos: true, buildName: 'e2e'}],
    ]
    : [['list'], ['html', {outputFolder: 'e2e-html-report', open: 'never'}]],
  use: {
    actionTimeout: 30_000,
    navigationTimeout: 60_000,
    trace: 'on',
    // 'only-on-failure', not 'on': Playwright's automatic end-of-test screenshot is uploaded to
    // Argos by the reporter under the test title, bypassing our argos-screenshot helper (so its
    // argosCSS version-masking never applies) and churning on the per-build app version. That
    // capture also just duplicates the controlled e2e-* snapshots the tests already take, so drop
    // it on passing runs. Traces stay on for debugging; failure screenshots are still captured.
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    viewport: {width: 1280, height: 720},
    // Kind cluster ingress uses a snake-oil cert.
    ignoreHTTPSErrors: true,
  },
  projects: [
    {name: 'chromium', use: devices['Desktop Chrome']},
  ],
});
