import {type Page} from '@playwright/test';
import type {E2ETestConfig} from '../types';
import {HomePage} from './home-page';

export async function logoutFromServer(page: Page, server: E2ETestConfig['lexboxServer']): Promise<void> {
  await new HomePage(page).ensureLoggedOut(server);
}

export async function deleteProject(page: Page, projectCode: string): Promise<void> {
  const origin = new URL(page.url()).origin;
  // Best-effort cleanup; the project may not exist if a test failed before downloading it.
  await page.request.delete(`${origin}/api/crdt/${projectCode}`).catch(() => undefined);
}

// Project IDs match the constants in `backend/LexData/SeedingData.cs`. Hardcoded
// because `/api/crdt/lookupProjectId` returns 406 when a project has no CRDT
// commits yet — exactly the state ensureProjectCrdtReady is here to resolve.
const SEEDED_PROJECT_IDS: Record<string, string> = {
  'sena-3': '0ebc5976-058d-4447-aaa7-297f8569f968',
  'elawa': '9e972940-8a8e-4b29-a609-bdc2f93b3507',
  'empty': '762b50e8-2e09-4ed4-a48d-775e1ada78e8',
};

/**
 * In a freshly-seeded lexbox, projects exist with FwData metadata only — no
 * `ServerCommit` rows. The FwLite home page filters listed projects to ones
 * with `IsCrdtProject=true`, so the project is invisible until an initial sync
 * runs. This calls lexbox's `/api/fw-lite/sync/trigger/{id}` endpoint (browser
 * cookie auth — `LexboxApi` scope satisfies the `RequireScope(SendAndReceive)`
 * check; the test user must have at least Editor role on the project).
 */
export async function ensureProjectCrdtReady(
  page: Page,
  server: E2ETestConfig['lexboxServer'],
  projectCode: string,
): Promise<void> {
  const projectId = SEEDED_PROJECT_IDS[projectCode];
  if (!projectId) {
    throw new Error(`Unknown project code "${projectCode}". Add to SEEDED_PROJECT_IDS or use a project that's already a CRDT project.`);
  }
  const lexboxBase = `${server.protocol}://${server.hostname}:${server.port}`;

  const trigger = await page.request.post(`${lexboxBase}/api/fw-lite/sync/trigger/${projectId}`);
  if (!trigger.ok()) throw new Error(`Trigger sync for "${projectCode}" failed: ${trigger.status()} ${await trigger.text()}`);

  const finish = await page.request.get(`${lexboxBase}/api/fw-lite/sync/await-sync-finished/${projectId}`, {timeout: 120_000});
  if (!finish.ok()) throw new Error(`Await sync finished for "${projectCode}" failed: ${finish.status()} ${await finish.text()}`);
}
