import * as testEnv from './envVars';

import { AdminDashboardPage } from './pages/adminDashboardPage';
import { ProjectPage } from './pages/projectPage';
import { loginAs } from './utils/authHelpers';
import { test, type CreateProjectResponse } from './fixtures';
import { waitForGqlResponse } from './utils/gqlHelpers';
import { expect } from '@playwright/test';

test('delete and recreate project', async ({ page, uniqueTestId }) => {
  // Step 1: Login
  await loginAs(page.request, 'admin', testEnv.defaultPassword);
  const adminDashboard = await new AdminDashboardPage(page).goto();

  // Step 2: Create a new project
  const createProjectPage = await adminDashboard.clickCreateProject();
  const projectCode = `recreate-project-test-${uniqueTestId}`;
  await createProjectPage.fillForm({ code: projectCode, customCode: true });
  await createProjectPage.submit();
  const projectPage = await new ProjectPage(page, projectCode, projectCode).waitFor();

  // Step 3: Delete the project
  const deleteProjectModal = await projectPage.clickDeleteProject();
  await deleteProjectModal.confirmDeleteProject();
  await adminDashboard.waitFor();

  // Step 4: Recreate the project
  await adminDashboard.clickCreateProject();
  await createProjectPage.fillForm({ code: projectCode, customCode: true });

  const createProjectResponse = await waitForGqlResponse<CreateProjectResponse>(page, async () => {
    await createProjectPage.submit();
  });
  await projectPage.waitFor();

  // Step 5: Clean up the last created project (the first one is only soft deleted, but that's probably fine)
  const projectId = createProjectResponse.data.createProject.createProjectResponse.id;
  const deleteResponse = await page.request.delete(`${testEnv.serverBaseUrl}/api/project/${projectId}`);
  expect(deleteResponse.ok()).toBeTruthy();
});
