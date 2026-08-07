import {type Page} from '@playwright/test';
import {serverUrl, type Server} from '../config';

// `/api/crdt/{code}` is a FwLite endpoint, so target FwLite explicitly rather than the
// page's current origin (which may still be the Lexbox host mid-OAuth).
export async function deleteProject(page: Page, fwLiteBaseUrl: string, projectCode: string): Promise<void> {
  const origin = new URL(fwLiteBaseUrl).origin;
  // Best-effort cleanup; the project may not exist if the test failed early.
  await page.request.delete(`${origin}/api/crdt/${projectCode}`).catch(() => undefined);
}

// IDs match `backend/LexData/SeedingData.cs`
const SEEDED_PROJECT_IDS: Record<string, string> = {
  'sena-3': '0ebc5976-058d-4447-aaa7-297f8569f968',
  'elawa-dev-flex': '9e972940-8a8e-4b29-a609-bdc2f93b3507',
  'empty-dev-flex': '762b50e8-2e09-4ed4-a48d-775e1ada78e8',
};

/**
 * Ensures the specified FieldWorks project has been made available in FieldWorks Lite.
 * Triggers the initial sync if necessary, which can take some time.
 * 
 * Uses browser cookie auth — the test user must have at least Editor role
 * (satisfies `RequireScope(SendAndReceive)`).
 */
export async function ensureProjectCrdtReady(
  page: Page,
  server: Server,
  projectCode: string,
): Promise<void> {
  const projectId = SEEDED_PROJECT_IDS[projectCode];
  if (!projectId) {
    throw new Error(`Unknown project code "${projectCode}". Add to SEEDED_PROJECT_IDS or use a project with existing CRDT commits.`);
  }
  const base = serverUrl(server);

  const trigger = await page.request.post(`${base}/api/fw-lite/sync/trigger/${projectId}`);
  if (!trigger.ok()) throw new Error(`Trigger sync for "${projectCode}" failed: ${trigger.status()} ${await trigger.text()}`);

  const finish = await page.request.get(`${base}/api/fw-lite/sync/await-sync-finished/${projectId}`, {timeout: 120_000});
  if (!finish.ok()) throw new Error(`Await sync finished for "${projectCode}" failed: ${finish.status()} ${await finish.text()}`);
}
