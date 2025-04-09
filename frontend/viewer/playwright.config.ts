import {defineConfig, devices} from '@playwright/test';
import * as testEnv from '../tests/envVars';
export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  /* Fail the build on CI if you accidentally left test.only in the source code. */
  forbidOnly: !!process.env.CI,
  /* Retry on CI only */
  retries: process.env.CI ? 1 : 0,
  /* Opt out of parallel tests on CI. */
  workers: process.env.CI ? 1 : 2,
  outputDir: 'test-results',
  /* Reporter to use. See https://playwright.dev/docs/test-reporters */
  reporter: process.env.CI
    ? [['github'], ['list'], ['junit', {outputFile: 'test-results/results.xml'}]]
    // Putting the HTML report in a subdirectory of the main output directory results in a warning log
    // stating that it will "lead to artifact loss" but the warning in this case is not accurate
    : [['list'], ['html', {outputFolder: 'html-test-results', open: 'never'}]],

  use: {
    baseURL: 'http://localhost:5137',
    /* Local storage to be populated for every test */
    storageState: {
      cookies: [],
      origins: [
        {
          origin: testEnv.serverHostname,
          localStorage: [
            {
              name: 'isPlaywright',
              value: 'true',
            },
            {
              name: 'shadcnMode',
              value: 'true'

            }
          ],
        },
      ],
    },
    viewport: {
      height: 720,
      width: 1280,
    }
  },
  projects: [
    {
      name: 'chromium',
      use: {...devices['Desktop Chrome'], userAgent: 'Playwright Chrome'},
    },
  ]
});
