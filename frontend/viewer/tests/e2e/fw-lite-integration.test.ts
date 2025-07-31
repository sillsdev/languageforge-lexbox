/**
 * FW Lite Integration E2E Tests
 *
 * This test suite implements the core integration scenarios for FW Lite and LexBox.
 * It tests the complete workflow: download project, create entry, delete local copy,
 * re-download, and verify entry persistence.
 */

import {expect, test} from '@playwright/test';
import {FwLiteLauncher} from './helpers/fw-lite-launcher';
import {
  deleteProject,
  logoutFromServer,
} from './helpers/project-operations';
import {
  cleanupTestData,
  generateTestEntry,
  generateUniqueIdentifier,
  getTestProject,
  validateTestDataConfiguration
} from './helpers/test-data';
import {getTestConfig} from './config';
import type {TestEntry, TestProject} from './types';
import {HomePage} from './helpers/home-page';
import { ProjectPage } from './helpers/project-page';

// Test configuration
const config = getTestConfig();
let fwLiteLauncher: FwLiteLauncher;
let testProject: TestProject;
let testEntry: TestEntry;
let testId: string;

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

    await page.goto(fwLiteLauncher.getBaseUrl());
    await page.waitForLoadState('networkidle');

    console.log('FW Lite application is ready for testing');
  });

  test.afterEach(async ({ page }) => {
    console.log('Cleaning up individual test');

    try {

      // Logout from server
      await logoutFromServer(page, config.lexboxServer);
    } catch (error) {
      console.warn('Cleanup warning:', error);
    }

    await deleteProject(page, 'sena-3');

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
   * Smoke test: Basic application launch and connectivity
   */
  test('Smoke test: Application launch and server connectivity', async ({ page }) => {
    const homePage = new HomePage(page);
    await test.step('Verify application is accessible', async () => {
      await homePage.waitFor();
    });

    await test.step('Verify server connectivity', async () => {
      // Attempt login to verify server connection
      await homePage.ensureLoggedIn(config.lexboxServer, config.testData.testUser, config.testData.testPassword);

      expect(await homePage.serverProjects(config.lexboxServer).count()).toBeGreaterThan(0);
    });
  });

  /**
   * Project download test: Isolated project download verification
   */
  test('Project download: Download and verify project structure', async ({ page }) => {
    test.setTimeout(1 * 60 * 1000);
    const homePage = new HomePage(page);

    await homePage.waitFor();
    await homePage.ensureLoggedIn(config.lexboxServer, config.testData.testUser, config.testData.testPassword);

    await homePage.downloadProject(config.lexboxServer, 'sena-3');

    await homePage.openLocalProject('sena-3');

    const projectPage = new ProjectPage(page, 'sena-3');
    await projectPage.waitFor();
  });
});
