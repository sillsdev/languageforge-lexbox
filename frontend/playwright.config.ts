import * as testEnv from './tests/envVars';

import { defineConfig, devices } from '@playwright/test';

import type { PlaywrightTestConfig } from '@playwright/test';

/**
 * Read environment variables from file.
 * https://github.com/motdotla/dotenv
 */
// require('dotenv').config();

/**
 * See https://playwright.dev/docs/test-configuration.
 */

const config: PlaywrightTestConfig = {
  testDir: './tests',
  /* Run tests in files in parallel */
  fullyParallel: true,
  /* Fail the build on CI if you accidentally left test.only in the source code. */
  forbidOnly: !!process.env.CI,
  timeout: testEnv.TEST_TIMEOUT,
  /* Retry on CI only */
  retries: process.env.CI ? 1 : 0,
  /* Opt out of parallel tests on CI. */
  workers: process.env.CI ? 1 : undefined,
  outputDir: 'test-results',
  /* Reporter to use. See https://playwright.dev/docs/test-reporters */
  reporter: process.env.CI
  ? [['github'], ['list']]
  // Putting the HTML report in a subdirectory of the main output directory results in a warning log
  // stating that it will "lead to artifact loss" but the warning in this case is not accurate
  : [['list'], ['html', { outputFolder: 'test-results/_html-report', open: 'never' }]],
  /* Shared settings for all the projects below. See https://playwright.dev/docs/api/class-testoptions. */
  use: {
    /* Base URL to use in actions like `await page.goto('/')`. */
    baseURL: testEnv.serverBaseUrl,

    /* Local storage to be populated for every test */
    storageState:
    {
      cookies: [],
      origins: [
        {
          origin: testEnv.serverHostname,
          localStorage: [
            {
              name: "isPlaywright",
              value: "true",
            },
          ],
        },
      ],
    },

    /* See https://playwright.dev/docs/trace-viewer */
    /* not 'on-first-retry', because then you don't have good traces to reference when debugging */
    trace: 'on',
    screenshot: 'on',
  },

  /* Configure projects for major browsers */
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'], userAgent: 'Playwright Chrome' },
    },

    {
      name: 'firefox',
      use: { ...devices['Desktop Firefox'], userAgent: 'Playwright Firefox' },
    },

    // Disabling testing on WebKit due to race conditions with browser context fixtures. 2024-02-02 RM
    // {
    //   name: 'webkit',
    //   use: { ...devices['Desktop Safari'] },
    // },

    /* Test against mobile viewports. */
    // {
    //   name: 'Mobile Chrome',
    //   use: { ...devices['Pixel 5'] },
    // },
    // {
    //   name: 'Mobile Safari',
    //   use: { ...devices['iPhone 12'] },
    // },

    /* Test against branded browsers. */
    // {
    //   name: 'Microsoft Edge',
    //   use: { ...devices['Desktop Edge'], channel: 'msedge' },
    // },
    // {
    //   name: 'Google Chrome',
    //   use: { ...devices['Desktop Chrome'], channel: 'chrome' },
    // },
  ],

  /* Run your local dev server before starting the tests */
  // Not needed here since we'll pretty much always be doing `task up` before development. 2024-02 RM
  // webServer: {
  //   command: 'npm run build && npm run preview',
  //   port: 4173,
  //   reuseExistingServer: !process.env.CI,
  // },
}

export default defineConfig(config);
