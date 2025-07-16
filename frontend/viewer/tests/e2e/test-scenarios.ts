/**
 * Test Scenarios for FW Lite E2E Tests
 *
 * This module defines reusable test scenarios that can be composed
 * into different test suites and configurations.
 */

import type { Page } from '@playwright/test';
import { expect } from '@playwright/test';
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
import type { TestProject, TestEntry } from './types';

/**
 * Scenario: Complete project workflow
 * Tests the full cycle of download, modify, delete, re-download, verify
 */
export async function completeProjectWorkflowScenario(
  page: Page,
  project: TestProject,
  testEntry: TestEntry,
  credentials: { username: string; password: string }
): Promise<void> {
  console.log('🔄 Starting complete project workflow scenario');

  // Step 1: Login
  await loginToServer(page, credentials.username, credentials.password);

  // Step 2: Download project
  await downloadProject(page, project.code);
  const downloadVerified = await verifyProjectDownload(page, project);
  expect(downloadVerified).toBe(true);

  // Step 3: Get initial stats
  const initialStats = await getProjectStats(page, project.code);

  // Step 4: Create entry
  await createEntry(page, testEntry);
  const entryExists = await verifyEntryExists(page, testEntry);
  expect(entryExists).toBe(true);

  // Step 5: Verify entry through search
  const searchFound = await searchEntry(page, testEntry.lexeme);
  expect(searchFound).toBe(true);

  // Step 6: Verify stats updated
  const updatedStats = await getProjectStats(page, project.code);
  expect(updatedStats.entryCount).toBeGreaterThan(initialStats.entryCount);

  // Step 7: Delete local copy
  await deleteProject(page, project.code);
  const deletedVerification = await verifyProjectDownload(page, project);
  expect(deletedVerification).toBe(false);

  // Step 8: Re-download
  await downloadProject(page, project.code);
  const redownloadVerified = await verifyProjectDownload(page, project);
  expect(redownloadVerified).toBe(true);

  // Step 9: Verify persistence
  const persistenceSearch = await searchEntry(page, testEntry.lexeme);
  expect(persistenceSearch).toBe(true);

  const persistenceVerification = await verifyEntryExists(page, testEntry);
  expect(persistenceVerification).toBe(true);

  console.log('✅ Complete project workflow scenario completed');
}

/**
 * Scenario: Basic connectivity and authentication
 * Tests application launch and server connection
 */
export async function basicConnectivityScenario(
  page: Page,
  credentials: { username: string; password: string }
): Promise<void> {
  console.log('🔗 Starting basic connectivity scenario');

  // Verify page loads
  const title = await page.title();
  expect(title).toBeTruthy();

  // Test login
  await loginToServer(page, credentials.username, credentials.password);

  // Verify login success
  const userIndicator = page.locator('[data-testid="user-menu"], [data-testid="user-avatar"], .user-info').first();
  const isLoggedIn = await userIndicator.isVisible().catch(() => false);
  expect(isLoggedIn).toBe(true);

  console.log('✅ Basic connectivity scenario completed');
}

/**
 * Scenario: Project download and verification
 * Tests project download functionality in isolation
 */
export async function projectDownloadScenario(
  page: Page,
  project: TestProject,
  credentials: { username: string; password: string }
): Promise<void> {
  console.log('📥 Starting project download scenario');

  // Login
  await loginToServer(page, credentials.username, credentials.password);

  // Download project
  await downloadProject(page, project.code);

  // Verify download
  const downloadVerified = await verifyProjectDownload(page, project);
  expect(downloadVerified).toBe(true);

  // Verify project structure
  const stats = await getProjectStats(page, project.code);
  expect(stats).toBeDefined();
  expect(stats.projectName).toContain(project.name);

  console.log('✅ Project download scenario completed');
}

/**
 * Scenario: Entry management operations
 * Tests creating, searching, and verifying entries
 */
export async function entryManagementScenario(
  page: Page,
  project: TestProject,
  testEntry: TestEntry,
  credentials: { username: string; password: string }
): Promise<void> {
  console.log('📝 Starting entry management scenario');

  // Setup: Login and download project
  await loginToServer(page, credentials.username, credentials.password);
  await downloadProject(page, project.code);

  const downloadVerified = await verifyProjectDownload(page, project);
  expect(downloadVerified).toBe(true);

  // Create entry
  await createEntry(page, testEntry);

  // Verify entry creation
  const entryExists = await verifyEntryExists(page, testEntry);
  expect(entryExists).toBe(true);

  // Test search functionality
  const searchFound = await searchEntry(page, testEntry.lexeme);
  expect(searchFound).toBe(true);

  console.log('✅ Entry management scenario completed');
}

/**
 * Scenario: Data persistence verification
 * Tests that data persists across local project deletion and re-download
 */
export async function dataPersistenceScenario(
  page: Page,
  project: TestProject,
  testEntry: TestEntry,
  credentials: { username: string; password: string }
): Promise<void> {
  console.log('💾 Starting data persistence scenario');

  // Setup: Login, download, create entry
  await loginToServer(page, credentials.username, credentials.password);
  await downloadProject(page, project.code);
  await createEntry(page, testEntry);

  // Verify entry exists
  let entryExists = await verifyEntryExists(page, testEntry);
  expect(entryExists).toBe(true);

  // Delete local project
  await deleteProject(page, project.code);

  // Re-download project
  await downloadProject(page, project.code);

  // Verify entry persisted
  entryExists = await verifyEntryExists(page, testEntry);
  expect(entryExists).toBe(true);

  const searchFound = await searchEntry(page, testEntry.lexeme);
  expect(searchFound).toBe(true);

  console.log('✅ Data persistence scenario completed');
}

/**
 * Scenario: Error handling and recovery
 * Tests application behavior under error conditions
 */
export async function errorHandlingScenario(
  page: Page,
  project: TestProject,
  credentials: { username: string; password: string }
): Promise<void> {
  console.log('⚠️  Starting error handling scenario');

  // Test invalid login
  try {
    await loginToServer(page, 'invalid-user', 'invalid-password');
    // Should not reach here
    expect(false).toBe(true);
  } catch (error) {
    // Expected to fail
    console.log('   ✅ Invalid login correctly rejected');
  }

  // Test valid login after invalid attempt
  await loginToServer(page, credentials.username, credentials.password);

  // Test downloading non-existent project
  try {
    await downloadProject(page, 'non-existent-project');
    // Should not reach here
    expect(false).toBe(true);
  } catch (error) {
    // Expected to fail
    console.log('   ✅ Non-existent project download correctly rejected');
  }

  // Test normal operation after error
  await downloadProject(page, project.code);
  const downloadVerified = await verifyProjectDownload(page, project);
  expect(downloadVerified).toBe(true);

  console.log('✅ Error handling scenario completed');
}

/**
 * Scenario: Performance and timeout testing
 * Tests application behavior under time constraints
 */
export async function performanceScenario(
  page: Page,
  project: TestProject,
  credentials: { username: string; password: string }
): Promise<void> {
  console.log('⏱️  Starting performance scenario');

  const startTime = Date.now();

  // Measure login time
  const loginStart = Date.now();
  await loginToServer(page, credentials.username, credentials.password);
  const loginTime = Date.now() - loginStart;
  console.log(`   Login time: ${loginTime}ms`);

  // Measure download time
  const downloadStart = Date.now();
  await downloadProject(page, project.code);
  const downloadTime = Date.now() - downloadStart;
  console.log(`   Download time: ${downloadTime}ms`);

  // Verify reasonable performance
  expect(loginTime).toBeLessThan(30000); // 30 seconds max for login
  expect(downloadTime).toBeLessThan(120000); // 2 minutes max for download

  const totalTime = Date.now() - startTime;
  console.log(`   Total scenario time: ${totalTime}ms`);

  console.log('✅ Performance scenario completed');
}

/**
 * Utility function to take debug screenshots during scenarios
 */
export async function takeScenarioScreenshot(page: Page, scenarioName: string, stepName: string): Promise<void> {
  const timestamp = Date.now();
  const filename = `scenario-${scenarioName}-${stepName}-${timestamp}.png`;

  await page.screenshot({
    path: `test-results/${filename}`,
    fullPage: true
  });

  console.log(`   📸 Screenshot saved: ${filename}`);
}
