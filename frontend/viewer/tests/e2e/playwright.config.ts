/**
 * Playwright Configuration for FW Lite E2E Tests
 *
 * This configuration is specifically for E2E integration tests that require
 * FW Lite application management and extended timeouts.
 */

import { defineConfig, devices } from '@playwright/test';
import { getTestConfig } from './config';

const testConfig = getTestConfig();

export default defineConfig({
  testDir: '.',
  testMatch: '**/*.test.ts',

  // E2E tests need more time due to application startup and complex workflows
  timeout: 300000, // 5 minutes per test

  expect: {
    timeout: 30000, // 30 seconds for assertions
  },

  // Sequential execution to avoid resource conflicts
  fullyParallel: false,
  workers: 1,

  // Retry failed tests once in CI
  retries: process.env.CI ? 1 : 0,

  // Fail fast on CI if test.only is left in code
  forbidOnly: !!process.env.CI,

  // Output configuration
  outputDir: 'test-results',

  // Reporter configuration
  reporter: process.env.CI
    ? [
        ['github'],
        ['list'],
        ['junit', { outputFile: 'test-results/e2e-results.xml' }],
        ['html', { outputFolder: 'test-results/e2e-html-report', open: 'never' }]
      ]
    : [
        ['list'],
        ['html', { outputFolder: 'test-results/e2e-html-report', open: 'never' }]
      ],

  use: {
    // No base URL since we'll be connecting to dynamically launched FW Lite
    baseURL: undefined,

    // Extended timeouts for E2E operations
    actionTimeout: 30000, // 30 seconds for actions
    navigationTimeout: 60000, // 60 seconds for navigation

    // Always capture traces and screenshots for debugging
    trace: 'on',
    screenshot: 'on',
    video: 'retain-on-failure',

    // Browser context settings
    viewport: { width: 1280, height: 720 },
    ignoreHTTPSErrors: true, // For self-signed certificates in test environments

    // Storage state for test isolation
    storageState: {
      cookies: [],
      origins: []
    }
  },

  // Browser projects
  projects: [
    {
      name: 'chromium',
      use: {
        ...devices['Desktop Chrome'],
        // Use a specific user agent to identify E2E tests
        userAgent: 'Playwright E2E Tests - Chrome'
      },
    },

    // Only run on Chrome for E2E tests to reduce complexity and execution time
    // Additional browsers can be added later if needed
  ],

  // Global setup and teardown
  globalSetup: require.resolve('./global-setup.ts'),
  globalTeardown: require.resolve('./global-teardown.ts'),
});
