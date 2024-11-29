import * as testEnv from './envVars';

import {AdminDashboardPage} from './pages/adminDashboardPage';
import {ProjectPage} from './pages/projectPage';
import {loginAs} from './utils/authHelpers';
import {test, type CreateProjectResponse} from './fixtures';
import {waitForGqlResponse} from './utils/gqlHelpers';
import {expect} from '@playwright/test';
import {UserDashboardPage} from './pages/userDashboardPage';
import {EmailSubjects} from './email/email-page';
import {UserAccountSettingsPage} from './pages/userAccountSettingsPage';

test('Request and approve draft project', async ({page, tempUser, uniqueTestId}) => {
  const userDashboardPage = await test.step('Login as user without project create permission', async () => {
    await loginAs(page.request, tempUser.email, tempUser.password);
    return await new UserDashboardPage(page).goto();
  });

  await test.step('Verify email so we can request a project', async () => {
    const emailPage = await tempUser.mailbox.openEmail(page, EmailSubjects.VerifyEmail);
    const pagePromise = emailPage.page.context().waitForEvent('page');
    await emailPage.clickVerifyEmail();
    const newPage = await pagePromise;
    await new UserAccountSettingsPage(newPage).waitFor();
    await newPage.close();
  });

  let project = await test.step('Request a new project', async () => {
    await userDashboardPage.page.reload();
    await userDashboardPage.waitFor();
    const requestProjectPage = await userDashboardPage.clickCreateProject();
    const projectCode = `draft-project-test-${uniqueTestId}`;
    const project = await requestProjectPage.fillForm({code: projectCode, purpose: 'Testing'}); // Software Developer is only available for admins
    await requestProjectPage.submit();
    await userDashboardPage.waitFor();
    return project;
  });

  const createProjectResponse = await test.step('Approve the project as admin', async () => {
    await loginAs(page.request, 'admin');
    const adminDashboard = await new AdminDashboardPage(page).goto();
    const approveProjectPage = await adminDashboard.openDraftProject(project.name);
    project = await approveProjectPage.fillForm({...project, purpose: 'Software Developer'}); // Software Developer, so that it can be hard deleted
    const createProjectResponse = await waitForGqlResponse<CreateProjectResponse>(page, async () => {
      await approveProjectPage.submit();
    });
    await new ProjectPage(page, project.name, project.code).waitFor();
    return createProjectResponse;
  });

  await test.step('Delete the project', async () => {
    const projectId = createProjectResponse.data.createProject.createProjectResponse.id;
    const deleteResponse = await page.request.delete(`${testEnv.serverBaseUrl}/api/project/${projectId}`);
    expect(deleteResponse.ok()).toBeTruthy();
  });
});
