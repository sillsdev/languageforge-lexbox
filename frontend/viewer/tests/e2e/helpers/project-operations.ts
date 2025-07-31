/**
 * Project Operations Helper
 *
 * This module provides functions for project download automation and management.
 * It handles UI interactions for downloading projects, creating entries, and verifying data.
 */

import {type Page} from '@playwright/test';
import type {E2ETestConfig} from '../types';
import {HomePage} from './home-page';


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
 * Delete a local project copy
 *
 * @param page - Playwright page object
 * @param projectCode - Code of the project to delete
 * @throws Error if deletion fails
 */
export async function deleteProject(page: Page, projectCode: string): Promise<void> {
  const origin = new URL(page.url()).origin;
  await page.request.delete(`${origin}/api/crdt/${projectCode}`).catch(() => {
  });
}
