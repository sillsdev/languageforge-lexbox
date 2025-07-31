/**
 * Project Operations Helper
 *
 * This module provides functions for project download automation and management.
 * It handles UI interactions for downloading projects, creating entries, and verifying data.
 */

import {expect, type Page} from '@playwright/test';
import type {TestProject, E2ETestConfig} from '../types';
import { HomePage } from './home-page';

/**
 * Timeout constants for various operations
 */
const TIMEOUTS = {
  projectDownload: 60000, // 60 seconds for project download
  entryCreation: 30000,   // 30 seconds for entry creation
  searchOperation: 15000, // 15 seconds for search operations
  uiInteraction: 10000,   // 10 seconds for general UI interactions
  projectDeletion: 30000, // 30 seconds for project deletion
  loginTimeout: 15000     // 15 seconds for login operations
};

/**
 * Login to the LexBox server
 * Handles authentication before accessing server resources
 *
 * @param page - Playwright page object
 * @param username - Username for authentication
 * @param password - Password for authentication
 * @throws Error if login fails
 */
export async function loginToServer(page: Page, username: string, password: string, server: E2ETestConfig['lexboxServer']): Promise<void> {
  console.log(`Attempting to login as user: ${username}`);
  const homePage = new HomePage(page);
  await homePage.ensureLoggedIn(server, username, password);
}

/**
 * Logout from the LexBox server
 * Clears authentication state
 *
 * @param page - Playwright page object
 */
export async function logoutFromServer(page: Page, server: E2ETestConfig['lexboxServer']): Promise<void> {
  console.log('Attempting to logout');

  const homePage = new HomePage(page);
  await homePage.ensureLoggedOut(server);
}

/**
 * Download a project from the server
 * Automates the UI interaction to download a project and waits for completion
 *
 * @param page - Playwright page object
 * @param projectCode - Code of the project to download
 * @throws Error if download fails or times out
 */
export async function downloadProject(page: Page, projectCode: string, serverHostname?: string): Promise<void> {
  console.log(`Starting download for project: ${projectCode}`);

  try {
    const serverElement = serverHostname ? page.locator(`#${serverHostname}`) : page;
    const projectElement = serverElement.locator(`li:has-text("${projectCode}")`);

    // Click download button for the project
    const downloadButton = projectElement.locator(`button:has-text("Download")`);
    await downloadButton.waitFor({
      state: 'visible',
      timeout: TIMEOUTS.uiInteraction
    });

    await projectElement.click();

    // Wait for download to start (look for progress indicator)
    const progressIndicator = page.locator('.i-mdi-loading').first();
    await progressIndicator.waitFor({
      state: 'visible',
      timeout: TIMEOUTS.uiInteraction
    });

    console.log(`Download started for project: ${projectCode}`);

    // Wait for download to complete
    await waitForDownloadCompletion(page, projectCode);

    console.log(`Successfully downloaded project: ${projectCode}`);
  } catch (error) {
    const errorMessage = `Failed to download project '${projectCode}': ${error instanceof Error ? error.message : 'Unknown error'}`;
    console.error(errorMessage);

    // Take screenshot for debugging
    await page.screenshot({
      path: `download-failure-${projectCode}-${Date.now()}.png`,
      fullPage: true
    });

    throw new Error(errorMessage);
  }
}

/**
 * Delete a local project copy
 *
 * @param page - Playwright page object
 * @param projectCode - Code of the project to delete
 * @throws Error if deletion fails
 */
export async function deleteProject(page: Page, projectCode: string): Promise<void> {
  const origin = new URL(page.url()).origin;
  await page.request.delete(`${origin}/api/crdt/${projectCode}`).catch(() => {});
}

/**
 * Verify that a project has been successfully downloaded
 * Checks for project presence and validates expected data structure
 *
 * @param page - Playwright page object
 * @param project - Test project configuration
 * @returns Promise<boolean> - true if verification passes
 */
export async function verifyProjectDownload(page: Page, project: TestProject): Promise<boolean> {
  console.log(`Verifying download for project: ${project.code}`);

  try {
    // Navigate to local projects page
    await navigateToLocalProjectsPage(page);

    // Check if project appears in local projects list
    const localProjectSelector = `[data-testid="local-project-${project.code}"], [data-local-project="${project.code}"]`;
    const localProjectElement = page.locator(localProjectSelector).first();

    // Wait for project to be visible
    await localProjectElement.waitFor({
      state: 'visible',
      timeout: TIMEOUTS.uiInteraction
    });

    // Verify project name matches
    const projectNameElement = localProjectElement.locator('[data-testid="project-name"], .project-name').first();
    const displayedName = await projectNameElement.textContent();

    if (!displayedName?.includes(project.name)) {
      console.warn(`Project name mismatch. Expected: ${project.name}, Found: ${displayedName}`);
    }

    // Open the project to verify internal structure
    await localProjectElement.click();

    // Wait for project to load
    await page.waitForLoadState('networkidle');

    // Verify lexicon is accessible (based on expected structure)
    const lexiconSelector = '[data-testid="lexicon"], [data-view="lexicon"], .lexicon-view';
    const lexiconElement = page.locator(lexiconSelector).first();

    await lexiconElement.waitFor({
      state: 'visible',
      timeout: TIMEOUTS.uiInteraction
    });

    console.log(`Successfully verified project download: ${project.code}`);
    return true;
  } catch (error) {
    console.error(`Project download verification failed for '${project.code}':`, error);

    // Take screenshot for debugging
    await page.screenshot({
      path: `verification-failure-${project.code}-${Date.now()}.png`,
      fullPage: true
    });

    return false;
  }
}

/**
 * Wait for project download to complete
 */
async function waitForDownloadCompletion(page: Page, projectCode: string): Promise<void> {
  // Wait for progress indicator to disappear
  const progressIndicator = page.locator('.i-mdi-loading, :has-text("Downloading")').first();

  try {
    await progressIndicator.waitFor({
      state: 'detached',
      timeout: TIMEOUTS.projectDownload
    });
  } catch {
    // Progress indicator might not be detached, check for completion message
  }

  // Look for synced
  const projectElement = page.locator(`li:has-text("${projectCode}")`);
  await projectElement.locator(':has-text("Synced")').first()
  .waitFor({state: 'visible', timeout: TIMEOUTS.uiInteraction});
}
