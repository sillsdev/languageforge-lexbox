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

/**
 * Project IDs of seed-data projects in the lexbox kind cluster.
 * These match the constants in `backend/LexData/SeedingData.cs`. We hardcode
 * here because `/api/crdt/lookupProjectId` returns 406 when the project has
 * no CRDT commits yet — i.e. exactly the state we need to resolve.
 */
export const SEEDED_PROJECT_IDS: Record<string, string> = {
  'sena-3': '0ebc5976-058d-4447-aaa7-297f8569f968',
  'elawa': '9e972940-8a8e-4b29-a609-bdc2f93b3507',
  'empty': '762b50e8-2e09-4ed4-a48d-775e1ada78e8',
};

/**
 * Make sure the given project has a server-side CRDT representation.
 *
 * In a freshly-seeded lexbox (e.g. a kind cluster spun up for CI) projects
 * exist in the database with FwData metadata only — no `ServerCommit` rows.
 * The FwLite home page filters listed projects to ones with `IsCrdtProject=true`
 * (i.e. ones with at least one CRDT commit), so the project is invisible until
 * an initial sync runs.
 *
 * This calls lexbox's `/api/fw-lite/sync/trigger/{id}` endpoint (browser cookie
 * auth — `LexboxApi` scope satisfies the `RequireScope(SendAndReceive,
 * exclusive: false)` check; the test user must have at least Editor role on
 * the project) and waits for the sync to finish.
 */
export async function ensureProjectCrdtReady(
  page: Page,
  server: E2ETestConfig['lexboxServer'],
  projectCode: string,
): Promise<void> {
  const lexboxBase = `${server.protocol}://${server.hostname}:${server.port}`;
  const projectId = SEEDED_PROJECT_IDS[projectCode];
  if (!projectId) {
    throw new Error(`Unknown project code "${projectCode}". Add to SEEDED_PROJECT_IDS or use a project that's already a CRDT project.`);
  }

  const trigger = await page.request.post(`${lexboxBase}/api/fw-lite/sync/trigger/${projectId}`);
  if (!trigger.ok()) {
    throw new Error(`Trigger sync for "${projectCode}" failed: ${trigger.status()} ${await trigger.text()}`);
  }

  const finish = await page.request.get(`${lexboxBase}/api/fw-lite/sync/await-sync-finished/${projectId}`, {timeout: 120_000});
  if (!finish.ok()) {
    throw new Error(`Await sync finished for "${projectCode}" failed: ${finish.status()} ${await finish.text()}`);
  }
  console.log(`Initial sync for ${projectCode} finished:`, await finish.text());
}
