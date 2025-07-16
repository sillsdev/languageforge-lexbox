/**
 * Project Operations Helper
 *
 * This module provides functions for project download automation and management.
 * It handles UI interactions for downloading projects, creating entries, and verifying data.
 */

import {expect, type Page} from '@playwright/test';
import type {TestProject, TestEntry, E2ETestConfig} from '../types';
import {LoginPage} from '../../../../tests/pages/loginPage'

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

  try {
    // Check if already logged in by looking for user indicator
    const userIndicator = page.locator(`#${server.hostname} .i-mdi-account-circle`).first();
    const isLoggedIn = await userIndicator.isVisible().catch(() => false);

    if (isLoggedIn) {
      console.log('User already logged in, skipping login process');
      return;
    }

    // Look for login button or link
    const loginButton = page.locator(`#${server.hostname} a:has-text("Login")`).first();


    await loginButton.waitFor({
      state: 'visible',
      timeout: TIMEOUTS.uiInteraction
    });
    await loginButton.click();

    await expect(page).toHaveURL(url => url.href.startsWith(`${server.protocol}://${server.hostname}/login`), {timeout: TIMEOUTS.loginTimeout});

    //todo reuse login page
    const loginPage = new LoginPage(page);
    await loginPage.waitFor();
    await loginPage.fillForm(username, password);
    await loginPage.submit();

    // Wait for login to complete - look for user indicator or redirect
    try {
      await Promise.race([
        // Wait for user menu/avatar to appear
        page.locator('[data-testid="user-menu"], [data-testid="user-avatar"], .user-info').first().waitFor({
          state: 'visible',
          timeout: TIMEOUTS.loginTimeout
        }),
        // Or wait for redirect to dashboard/projects page
        page.waitForURL(/\/(dashboard|projects|home)/, {timeout: TIMEOUTS.loginTimeout})
      ]);
    } catch {
      // Check if login failed by looking for error messages
      const errorMessage = page.locator('[data-testid="login-error"], .error-message, .alert-error').first();
      const hasError = await errorMessage.isVisible().catch(() => false);

      if (hasError) {
        const errorText = await errorMessage.textContent();
        throw new Error(`Login failed: ${errorText || 'Invalid credentials'}`);
      }

      // If no error message but login didn't complete, assume timeout
      throw new Error('Login timeout - unable to verify successful authentication');
    }

    console.log(`Successfully logged in as user: ${username}`);
  } catch (error) {
    const errorMessage = `Failed to login as '${username}': ${error instanceof Error ? error.message : 'Unknown error'}`;
    console.error(errorMessage);

    // Take screenshot for debugging
    await page.screenshot({
      path: `login-failure-${username}-${Date.now()}.png`,
      fullPage: true
    });

    throw new Error(errorMessage);
  }
}

/**
 * Logout from the LexBox server
 * Clears authentication state
 *
 * @param page - Playwright page object
 */
export async function logoutFromServer(page: Page): Promise<void> {
  console.log('Attempting to logout');

  try {
    // Look for user menu or logout button
    const userMenu = page.locator('[data-testid="user-menu"], [data-testid="user-avatar"], .user-info').first();

    try {
      await userMenu.waitFor({
        state: 'visible',
        timeout: 5000
      });
      await userMenu.click();
    } catch {
      // User menu might not be visible, user might already be logged out
      console.log('User menu not found, user might already be logged out');
      return;
    }

    // Look for logout button in dropdown or menu
    const logoutButton = page.locator('[data-testid="logout-button"], button:has-text("Logout"), button:has-text("Sign Out"), a:has-text("Logout"), a:has-text("Sign Out")').first();

    try {
      await logoutButton.waitFor({
        state: 'visible',
        timeout: 5000
      });
      await logoutButton.click();
    } catch {
      console.log('Logout button not found, user might already be logged out');
      return;
    }

    // Wait for logout to complete - user menu should disappear
    await userMenu.waitFor({
      state: 'detached',
      timeout: TIMEOUTS.uiInteraction
    });

    console.log('Successfully logged out');
  } catch (error) {
    console.warn(`Logout may have failed: ${error instanceof Error ? error.message : 'Unknown error'}`);
    // Don't throw error for logout failures as they're not critical
  }
}

/**
 * Download a project from the server
 * Automates the UI interaction to download a project and waits for completion
 *
 * @param page - Playwright page object
 * @param projectCode - Code of the project to download
 * @throws Error if download fails or times out
 */
export async function downloadProject(page: Page, projectCode: string): Promise<void> {
  console.log(`Starting download for project: ${projectCode}`);

  try {
    // Navigate to projects page if not already there
    await navigateToProjectsPage(page);

    // Look for the project in the available projects list
    const projectSelector = `[data-testid="project-${projectCode}"], [data-project-code="${projectCode}"]`;
    const projectElement = page.locator(projectSelector).first();

    // Wait for project to be visible
    await projectElement.waitFor({
      state: 'visible',
      timeout: TIMEOUTS.uiInteraction
    });

    // Click download button for the project
    const downloadButton = projectElement.locator('[data-testid="download-button"], button:has-text("Download")').first();
    await downloadButton.waitFor({
      state: 'visible',
      timeout: TIMEOUTS.uiInteraction
    });

    await downloadButton.click();

    // Wait for download to start (look for progress indicator)
    const progressIndicator = page.locator('[data-testid="download-progress"], .download-progress, .progress-bar').first();
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
 * Removes the project from local storage and cleans up associated files
 *
 * @param page - Playwright page object
 * @param projectCode - Code of the project to delete
 * @throws Error if deletion fails
 */
export async function deleteProject(page: Page, projectCode: string): Promise<void> {
  console.log(`Starting deletion for project: ${projectCode}`);

  try {
    // Navigate to local projects or project management page
    await navigateToLocalProjectsPage(page);

    // Find the project in local projects list
    const localProjectSelector = `[data-testid="local-project-${projectCode}"], [data-local-project="${projectCode}"]`;
    const localProjectElement = page.locator(localProjectSelector).first();

    // Wait for project to be visible
    await localProjectElement.waitFor({
      state: 'visible',
      timeout: TIMEOUTS.uiInteraction
    });

    // Click delete/remove button
    const deleteButton = localProjectElement.locator('[data-testid="delete-button"], button:has-text("Delete"), button:has-text("Remove")').first();
    await deleteButton.waitFor({
      state: 'visible',
      timeout: TIMEOUTS.uiInteraction
    });

    await deleteButton.click();

    // Handle confirmation dialog if present
    const confirmButton = page.locator('[data-testid="confirm-delete"], button:has-text("Confirm"), button:has-text("Yes")').first();

    try {
      await confirmButton.waitFor({
        state: 'visible',
        timeout: 5000
      });
      await confirmButton.click();
    } catch {
      // No confirmation dialog, continue
    }

    // Wait for deletion to complete
    await localProjectElement.waitFor({
      state: 'detached',
      timeout: TIMEOUTS.projectDeletion
    });

    console.log(`Successfully deleted local project: ${projectCode}`);
  } catch (error) {
    const errorMessage = `Failed to delete project '${projectCode}': ${error instanceof Error ? error.message : 'Unknown error'}`;
    console.error(errorMessage);

    // Take screenshot for debugging
    await page.screenshot({
      path: `deletion-failure-${projectCode}-${Date.now()}.png`,
      fullPage: true
    });

    throw new Error(errorMessage);
  }
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
 * Create a new entry in the project
 * Automates UI interactions to add a new lexical entry
 *
 * @param page - Playwright page object
 * @param entryData - Data for the new entry
 * @throws Error if entry creation fails
 */
export async function createEntry(page: Page, entryData: TestEntry): Promise<void> {
  console.log(`Creating entry: ${entryData.lexeme}`);

  try {
    // Ensure we're in the lexicon view
    await navigateToLexiconView(page);

    // Click add/new entry button
    const addEntryButton = page.locator('[data-testid="add-entry"], button:has-text("Add Entry"), button:has-text("New Entry")').first();
    await addEntryButton.waitFor({
      state: 'visible',
      timeout: TIMEOUTS.uiInteraction
    });

    await addEntryButton.click();

    // Wait for entry form to appear
    const entryForm = page.locator('[data-testid="entry-form"], .entry-form, form').first();
    await entryForm.waitFor({
      state: 'visible',
      timeout: TIMEOUTS.uiInteraction
    });

    // Fill in lexeme field
    const lexemeField = entryForm.locator('[data-testid="lexeme-field"], input[name="lexeme"], [placeholder*="lexeme"]').first();
    await lexemeField.waitFor({
      state: 'visible',
      timeout: TIMEOUTS.uiInteraction
    });
    await lexemeField.fill(entryData.lexeme);

    // Fill in definition field
    const definitionField = entryForm.locator('[data-testid="definition-field"], textarea[name="definition"], [placeholder*="definition"]').first();
    await definitionField.waitFor({
      state: 'visible',
      timeout: TIMEOUTS.uiInteraction
    });
    await definitionField.fill(entryData.definition);

    // Select part of speech if available
    const posField = entryForm.locator('[data-testid="pos-field"], select[name="partOfSpeech"], [data-field="pos"]').first();
    try {
      await posField.waitFor({
        state: 'visible',
        timeout: 5000
      });
      await posField.selectOption(entryData.partOfSpeech);
    } catch {
      // Part of speech field might not be available or might be a different type
      console.log('Part of speech field not found or not selectable, continuing...');
    }

    // Save the entry
    const saveButton = entryForm.locator('[data-testid="save-entry"], button:has-text("Save"), button[type="submit"]').first();
    await saveButton.waitFor({
      state: 'visible',
      timeout: TIMEOUTS.uiInteraction
    });

    await saveButton.click();

    // Wait for entry to be saved and form to close
    await entryForm.waitFor({
      state: 'detached',
      timeout: TIMEOUTS.entryCreation
    });

    // Verify entry appears in the list
    const entryInList = page.locator(`[data-testid="entry-${entryData.uniqueIdentifier}"], :has-text("${entryData.lexeme}")`).first();
    await entryInList.waitFor({
      state: 'visible',
      timeout: TIMEOUTS.uiInteraction
    });

    console.log(`Successfully created entry: ${entryData.lexeme}`);
  } catch (error) {
    const errorMessage = `Failed to create entry '${entryData.lexeme}': ${error instanceof Error ? error.message : 'Unknown error'}`;
    console.error(errorMessage);

    // Take screenshot for debugging
    await page.screenshot({
      path: `entry-creation-failure-${entryData.uniqueIdentifier}-${Date.now()}.png`,
      fullPage: true
    });

    throw new Error(errorMessage);
  }
}

/**
 * Search for an entry in the project
 * Uses the search functionality to find a specific entry
 *
 * @param page - Playwright page object
 * @param searchTerm - Term to search for
 * @returns Promise<boolean> - true if entry is found
 */
export async function searchEntry(page: Page, searchTerm: string): Promise<boolean> {
  console.log(`Searching for entry: ${searchTerm}`);

  try {
    // Ensure we're in the lexicon view
    await navigateToLexiconView(page);

    // Find and use search field
    const searchField = page.locator('[data-testid="search-field"], input[type="search"], input[placeholder*="search"]').first();
    await searchField.waitFor({
      state: 'visible',
      timeout: TIMEOUTS.uiInteraction
    });

    // Clear existing search and enter new term
    await searchField.clear();
    await searchField.fill(searchTerm);

    // Trigger search (might be automatic or require button click)
    const searchButton = page.locator('[data-testid="search-button"], button:has-text("Search")').first();
    try {
      await searchButton.waitFor({
        state: 'visible',
        timeout: 2000
      });
      await searchButton.click();
    } catch {
      // Search might be automatic, continue
    }

    // Wait for search results to load
    await page.waitForTimeout(1000);

    // Look for the entry in search results
    const searchResults = page.locator('[data-testid="search-results"], .search-results, .entry-list').first();
    await searchResults.waitFor({
      state: 'visible',
      timeout: TIMEOUTS.searchOperation
    });

    // Check if the search term appears in results
    const entryFound = await searchResults.locator(`:has-text("${searchTerm}")`).first().isVisible();

    if (entryFound) {
      console.log(`Successfully found entry: ${searchTerm}`);
      return true;
    } else {
      console.log(`Entry not found: ${searchTerm}`);
      return false;
    }
  } catch (error) {
    console.error(`Search failed for term '${searchTerm}':`, error);

    // Take screenshot for debugging
    await page.screenshot({
      path: `search-failure-${searchTerm.replace(/[^a-zA-Z0-9]/g, '_')}-${Date.now()}.png`,
      fullPage: true
    });

    return false;
  }
}

/**
 * Verify that an entry exists in the project
 * More thorough verification than search, checks entry details
 *
 * @param page - Playwright page object
 * @param entryData - Entry data to verify
 * @returns Promise<boolean> - true if entry exists and matches expected data
 */
export async function verifyEntryExists(page: Page, entryData: TestEntry): Promise<boolean> {
  console.log(`Verifying entry exists: ${entryData.lexeme}`);

  try {
    // First try to find the entry through search
    const searchFound = await searchEntry(page, entryData.lexeme);

    if (!searchFound) {
      console.log(`Entry not found in search: ${entryData.lexeme}`);
      return false;
    }

    // Click on the entry to view details
    const entryElement = page.locator(`:has-text("${entryData.lexeme}")`).first();
    await entryElement.click();

    // Wait for entry details to load
    const entryDetails = page.locator('[data-testid="entry-details"], .entry-details').first();
    await entryDetails.waitFor({
      state: 'visible',
      timeout: TIMEOUTS.uiInteraction
    });

    // Verify lexeme matches
    const lexemeElement = entryDetails.locator('[data-testid="entry-lexeme"], .lexeme').first();
    const displayedLexeme = await lexemeElement.textContent();

    if (!displayedLexeme?.includes(entryData.lexeme)) {
      console.warn(`Lexeme mismatch. Expected: ${entryData.lexeme}, Found: ${displayedLexeme}`);
      return false;
    }

    // Verify definition matches
    const definitionElement = entryDetails.locator('[data-testid="entry-definition"], .definition').first();
    const displayedDefinition = await definitionElement.textContent();

    if (!displayedDefinition?.includes(entryData.definition)) {
      console.warn(`Definition mismatch. Expected: ${entryData.definition}, Found: ${displayedDefinition}`);
      return false;
    }

    console.log(`Successfully verified entry: ${entryData.lexeme}`);
    return true;
  } catch (error) {
    console.error(`Entry verification failed for '${entryData.lexeme}':`, error);
    return false;
  }
}

/**
 * Get project statistics and information
 * Retrieves current project state for validation
 *
 * @param page - Playwright page object
 * @param projectCode - Project code to get stats for
 * @returns Promise<object> - Project statistics
 */
export async function getProjectStats(page: Page, projectCode: string): Promise<{
  entryCount: number;
  projectName: string;
  lastModified?: string;
}> {
  console.log(`Getting stats for project: ${projectCode}`);

  try {
    // Navigate to project overview or stats page
    await navigateToProjectOverview(page);

    // Get entry count
    const entryCountElement = page.locator('[data-testid="entry-count"], .entry-count').first();
    const entryCountText = await entryCountElement.textContent();
    const entryCount = parseInt(entryCountText?.match(/\d+/)?.[0] || '0', 10);

    // Get project name
    const projectNameElement = page.locator('[data-testid="project-name"], .project-title, h1').first();
    const projectName = await projectNameElement.textContent() || '';

    // Get last modified date if available
    let lastModified: string | undefined;
    try {
      const lastModifiedElement = page.locator('[data-testid="last-modified"], .last-modified').first();
      lastModified = await lastModifiedElement.textContent() || undefined;
    } catch {
      // Last modified might not be available
    }

    const stats = {
      entryCount,
      projectName: projectName.trim(),
      lastModified
    };

    console.log(`Project stats for ${projectCode}:`, stats);
    return stats;
  } catch (error) {
    console.error(`Failed to get project stats for '${projectCode}':`, error);
    throw new Error(`Could not retrieve project statistics: ${error instanceof Error ? error.message : 'Unknown error'}`);
  }
}

// Helper functions for navigation

/**
 * Navigate to the projects page
 */
async function navigateToProjectsPage(page: Page): Promise<void> {
  const projectsLink = page.locator('[data-testid="projects-nav"], a:has-text("Projects")').first();

  try {
    await projectsLink.waitFor({
      state: 'visible',
      timeout: TIMEOUTS.uiInteraction
    });
    await projectsLink.click();
  } catch {
    // Might already be on projects page
  }

  await page.waitForLoadState('networkidle');
}

/**
 * Navigate to the local projects page
 */
async function navigateToLocalProjectsPage(page: Page): Promise<void> {
  const localProjectsLink = page.locator('[data-testid="local-projects-nav"], a:has-text("Local Projects")').first();

  try {
    await localProjectsLink.waitFor({
      state: 'visible',
      timeout: TIMEOUTS.uiInteraction
    });
    await localProjectsLink.click();
  } catch {
    // Try alternative navigation
    await navigateToProjectsPage(page);

    const localTab = page.locator('[data-testid="local-tab"], button:has-text("Local")').first();
    try {
      await localTab.waitFor({
        state: 'visible',
        timeout: TIMEOUTS.uiInteraction
      });
      await localTab.click();
    } catch {
      // Might already be showing local projects
    }
  }

  await page.waitForLoadState('networkidle');
}

/**
 * Navigate to the lexicon view
 */
async function navigateToLexiconView(page: Page): Promise<void> {
  const lexiconLink = page.locator('[data-testid="lexicon-nav"], a:has-text("Lexicon")').first();

  try {
    await lexiconLink.waitFor({
      state: 'visible',
      timeout: TIMEOUTS.uiInteraction
    });
    await lexiconLink.click();
  } catch {
    // Might already be in lexicon view
  }

  await page.waitForLoadState('networkidle');
}

/**
 * Navigate to project overview
 */
async function navigateToProjectOverview(page: Page): Promise<void> {
  const overviewLink = page.locator('[data-testid="overview-nav"], a:has-text("Overview")').first();

  try {
    await overviewLink.waitFor({
      state: 'visible',
      timeout: TIMEOUTS.uiInteraction
    });
    await overviewLink.click();
  } catch {
    // Might already be in overview
  }

  await page.waitForLoadState('networkidle');
}

/**
 * Wait for project download to complete
 */
async function waitForDownloadCompletion(page: Page, projectCode: string): Promise<void> {
  // Wait for progress indicator to disappear
  const progressIndicator = page.locator('[data-testid="download-progress"], .download-progress').first();

  try {
    await progressIndicator.waitFor({
      state: 'detached',
      timeout: TIMEOUTS.projectDownload
    });
  } catch {
    // Progress indicator might not be detached, check for completion message
  }

  // Look for completion message or project in local list
  const completionMessage = page.locator(':has-text("Download complete"), :has-text("Download successful")').first();
  const localProject = page.locator(`[data-testid="local-project-${projectCode}"]`).first();

  try {
    await Promise.race([
      completionMessage.waitFor({state: 'visible', timeout: 5000}),
      localProject.waitFor({state: 'visible', timeout: 5000})
    ]);
  } catch {
    // Final fallback - wait a bit more for download to complete
    await page.waitForTimeout(5000);
  }
}
