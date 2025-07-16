/**
 * FW Lite Integration E2E Tests
 *
 * This test suite implements the core integration scenarios for FW Lite and LexBox.
 * It tests the complete workflow: download project, create entry, delete local copy,
 * re-download, and verify entry persistence.
 */

import { test, expect, type Page } from '@playwright/test';
import { FwLiteLauncher } from './helpers/fw-lite-launcher';
import {
  loginToServer,
  logoutFromServer,
  downloadProject,
  deleteProject,
  verifyProjectDownload,
  createEntry,
  searchEntry,
  verifyEntryExists,
  getProjectStats
} from './helpers/project-operations';
import {
  getTestProject,
  generateTestEntry,
  generateUniqueIdentifier,
  cleanupTestData,
  validateTestDataConfiguration
} from './helpers/test-data';
import { getTestConfig } from './config';
import type { E2ETestConfig, TestEntry, TestProject } from './types';

// Test configuration
const config = getTestConfig();
let fwLiteLauncher: FwLiteLauncher;
let testProject: TestProject;
let testEntry: TestEntry;
let testId: string;

/**
 * Page Object Model for FW Lite UI interactions
 */
class FwLitePageObject {
  constructor(private page: Page, private config: E2ETestConfig) {}

  /**
   * Navigate to the FW Lite application
   */
  async navigateToApp(): Promise<void> {
    await this.page.goto('/');
    await this.page.waitForLoadState('networkidle');
  }

  /**
   * Wait for application to be ready
   */
  async waitForAppReady(): Promise<void> {
    // Wait for main application container to be visible
    await this.page.waitForSelector('body', { timeout: 30000 });

    // Wait for any loading indicators to disappear
    const loadingIndicators = this.page.locator('.loading, [data-testid="loading"], .spinner');
    try {
      await loadingIndicators.waitFor({ state: 'detached', timeout: 10000 });
    } catch {
      // Loading indicators might not exist, continue
    }

    // Ensure page is interactive
    await this.page.waitForLoadState('networkidle');
  }

  /**
   * Take a screenshot for debugging
   */
  async takeDebugScreenshot(name: string): Promise<void> {
    await this.page.screenshot({
      path: `test-results/debug-${name}-${Date.now()}.png`,
      fullPage: true
    });
  }

  /**
   * Get page title for verification
   */
  async getPageTitle(): Promise<string> {
    return await this.page.title();
  }

  /**
   * Check if user is logged in
   */
  async isUserLoggedIn(): Promise<boolean> {
    const userIndicator = this.page.locator(`#${this.config.lexboxServer.hostname} .i-mdi-account-circle`).first();
    return await userIndicator.isVisible().catch(() => false);
  }

  /**
   * Get current URL for verification
   */
  getCurrentUrl(): string {
    return this.page.url();
  }
}

/**
 * Test suite setup and teardown
 */
test.describe('FW Lite Integration Tests', () => {
  test.beforeAll(async () => {
    console.log('Setting up FW Lite Integration Test Suite');

    // Validate test configuration
    validateTestDataConfiguration(config.testData.projectCode);

    // Get test project configuration
    testProject = getTestProject(config.testData.projectCode);

    // Generate unique test identifier
    testId = generateUniqueIdentifier('integration');

    // Generate test entry data
    testEntry = generateTestEntry(testId, 'basic');

    console.log('Test configuration:', {
      project: testProject.code,
      testId,
      entry: testEntry.lexeme
    });
  });

  test.beforeEach(async ({ page }) => {
    console.log('Setting up individual test');

    // Initialize FW Lite launcher
    fwLiteLauncher = new FwLiteLauncher();

    // Launch FW Lite application
    await fwLiteLauncher.launch({
      binaryPath: config.fwLite.binaryPath,
      serverUrl: `${config.lexboxServer.protocol}://${config.lexboxServer.hostname}`,
      timeout: config.fwLite.launchTimeout
    });

    console.log(`FW Lite launched at: ${fwLiteLauncher.getBaseUrl()}`);

    // Navigate to the application
    const pageObject = new FwLitePageObject(page, config);
    await page.goto(fwLiteLauncher.getBaseUrl());
    await pageObject.waitForAppReady();

    console.log('FW Lite application is ready for testing');
  });

  test.afterEach(async ({ page }) => {
    console.log('Cleaning up individual test');

    try {
      // Take final screenshot for debugging if test failed
      const pageObject = new FwLitePageObject(page, config);
      await pageObject.takeDebugScreenshot('test-cleanup');

      // Logout from server
      await logoutFromServer(page, config.lexboxServer.hostname);
    } catch (error) {
      console.warn('Cleanup warning:', error);
    }

    // Shutdown FW Lite application
    if (fwLiteLauncher) {
      await fwLiteLauncher.shutdown();
      console.log('FW Lite application shut down');
    }
  });

  test.afterAll(async () => {
    console.log('Cleaning up test suite');

    // Clean up test data
    try {
      cleanupTestData(testProject.code, [testId]);
      console.log('Test data cleanup completed');
    } catch (error) {
      console.warn('Test data cleanup warning:', error);
    }
  });

  /**
   * Main integration test: Complete workflow from download to verification
   */
  test('Complete project workflow: download, modify, sync, verify', async ({ page }) => {
    const pageObject = new FwLitePageObject(page, config);

    console.log('Starting complete project workflow test');

    // Step 1: Login to server
    console.log('Step 1: Logging in to server');
    await test.step('Login to LexBox server', async () => {
      await loginToServer(page, config.testData.testUser, config.testData.testPassword, config.lexboxServer);

      // Verify login was successful
      const isLoggedIn = await pageObject.isUserLoggedIn();
      expect(isLoggedIn).toBe(true);

      await pageObject.takeDebugScreenshot('after-login');
    });

    // Step 2: Download project
    console.log('Step 2: Downloading project');
    await test.step('Download test project', async () => {
      await downloadProject(page, testProject.code);

      // Verify project was downloaded successfully
      const downloadVerified = await verifyProjectDownload(page, testProject);
      expect(downloadVerified).toBe(true);

      await pageObject.takeDebugScreenshot('after-download');
    });

    // Step 3: Get initial project statistics
    console.log('Step 3: Getting initial project statistics');
    let initialStats: any;
    await test.step('Get initial project statistics', async () => {
      initialStats = await getProjectStats(page, testProject.code);

      expect(initialStats).toBeDefined();
      expect(initialStats.projectName).toContain(testProject.name);

      console.log('Initial project stats:', initialStats);
    });

    // Step 4: Create new entry
    console.log('Step 4: Creating new entry');
    await test.step('Create new lexical entry', async () => {
      await createEntry(page, testEntry);

      // Verify entry was created successfully
      const entryExists = await verifyEntryExists(page, testEntry);
      expect(entryExists).toBe(true);

      await pageObject.takeDebugScreenshot('after-entry-creation');
    });

    // Step 5: Verify entry can be found through search
    console.log('Step 5: Verifying entry through search');
    await test.step('Search for created entry', async () => {
      const searchFound = await searchEntry(page, testEntry.lexeme);
      expect(searchFound).toBe(true);

      await pageObject.takeDebugScreenshot('after-search');
    });

    // Step 6: Get updated project statistics
    console.log('Step 6: Getting updated project statistics');
    let updatedStats: any;
    await test.step('Verify project statistics updated', async () => {
      updatedStats = await getProjectStats(page, testProject.code);

      expect(updatedStats).toBeDefined();
      expect(updatedStats.entryCount).toBeGreaterThan(initialStats.entryCount);

      console.log('Updated project stats:', updatedStats);
    });

    // Step 7: Delete local project copy
    console.log('Step 7: Deleting local project copy');
    await test.step('Delete local project copy', async () => {
      await deleteProject(page, testProject.code);

      // Verify project was deleted locally
      const downloadVerified = await verifyProjectDownload(page, testProject);
      expect(downloadVerified).toBe(false);

      await pageObject.takeDebugScreenshot('after-deletion');
    });

    // Step 8: Re-download project
    console.log('Step 8: Re-downloading project');
    await test.step('Re-download project from server', async () => {
      await downloadProject(page, testProject.code);

      // Verify project was re-downloaded successfully
      const redownloadVerified = await verifyProjectDownload(page, testProject);
      expect(redownloadVerified).toBe(true);

      await pageObject.takeDebugScreenshot('after-redownload');
    });

    // Step 9: Verify entry persisted after re-download
    console.log('Step 9: Verifying entry persistence');
    await test.step('Verify entry persisted after re-download', async () => {
      // Search for the entry that was created before deletion
      const searchFound = await searchEntry(page, testEntry.lexeme);
      expect(searchFound).toBe(true);

      // Verify entry details are intact
      const entryExists = await verifyEntryExists(page, testEntry);
      expect(entryExists).toBe(true);

      await pageObject.takeDebugScreenshot('after-persistence-verification');
    });

    // Step 10: Final project statistics verification
    console.log('Step 10: Final verification');
    await test.step('Final project statistics verification', async () => {
      const finalStats = await getProjectStats(page, testProject.code);

      expect(finalStats).toBeDefined();
      expect(finalStats.entryCount).toBe(updatedStats.entryCount);
      expect(finalStats.entryCount).toBeGreaterThan(initialStats.entryCount);

      console.log('Final project stats:', finalStats);
      console.log('Test completed successfully!');
    });
  });

  /**
   * Smoke test: Basic application launch and connectivity
   */
  test('Smoke test: Application launch and server connectivity', async ({ page }) => {
    const pageObject = new FwLitePageObject(page, config);

    console.log('Starting smoke test');

    await test.step('Verify application is accessible', async () => {
      // Verify page loads
      const title = await pageObject.getPageTitle();
      expect(title).toBeTruthy();

      // Verify URL is correct
      const currentUrl = pageObject.getCurrentUrl();
      expect(currentUrl).toContain(fwLiteLauncher.getBaseUrl());

      await pageObject.takeDebugScreenshot('smoke-test-loaded');
    });

    await test.step('Verify server connectivity', async () => {
      // Attempt login to verify server connection
      await loginToServer(page, config.testData.testUser, config.testData.testPassword, config.lexboxServer);

      // Verify login was successful
      const isLoggedIn = await pageObject.isUserLoggedIn();
      expect(isLoggedIn).toBe(true);

      await pageObject.takeDebugScreenshot('smoke-test-connected');
    });

    console.log('Smoke test completed successfully');
  });

  /**
   * Project download test: Isolated project download verification
   */
  test('Project download: Download and verify project structure', async ({ page }) => {
    test.setTimeout(1 * 60 * 1000);
    const pageObject = new FwLitePageObject(page, config);

    console.log('Starting project download test');

    await test.step('Login and download project', async () => {
      // Login to server
      await loginToServer(page, config.testData.testUser, config.testData.testPassword, config.lexboxServer);

      // Download project
      await downloadProject(page, testProject.code, config.lexboxServer.hostname);

      await pageObject.takeDebugScreenshot('download-test-completed');
    });

    await test.step('Verify project structure', async () => {
      // Verify project was downloaded
      const downloadVerified = await verifyProjectDownload(page, testProject);
      expect(downloadVerified).toBe(true);

      // Get project statistics
      const stats = await getProjectStats(page, testProject.code);
      expect(stats).toBeDefined();
      expect(stats.projectName).toContain(testProject.name);

      console.log('Project download test completed successfully');
    });
  });

  /**
   * Entry management test: Create and search entries
   */
  test('Entry management: Create, search, and verify entries', async ({ page }) => {
    const pageObject = new FwLitePageObject(page, config);

    // Generate unique test entry for this test
    const entryTestId = generateUniqueIdentifier('entry-mgmt');
    const entryTestData = generateTestEntry(entryTestId, 'verb');

    console.log('Starting entry management test');

    await test.step('Setup: Login and download project', async () => {
      await loginToServer(page, config.testData.testUser, config.testData.testPassword, config.lexboxServer);
      await downloadProject(page, testProject.code);

      const downloadVerified = await verifyProjectDownload(page, testProject);
      expect(downloadVerified).toBe(true);
    });

    await test.step('Create new entry', async () => {
      await createEntry(page, entryTestData);

      // Verify entry was created
      const entryExists = await verifyEntryExists(page, entryTestData);
      expect(entryExists).toBe(true);

      await pageObject.takeDebugScreenshot('entry-created');
    });

    await test.step('Search for entry', async () => {
      const searchFound = await searchEntry(page, entryTestData.lexeme);
      expect(searchFound).toBe(true);

      await pageObject.takeDebugScreenshot('entry-searched');
    });

    await test.step('Cleanup test entry', async () => {
      // Clean up the test entry
      cleanupTestData(testProject.code, [entryTestId]);

      console.log('Entry management test completed successfully');
    });
  });
});
