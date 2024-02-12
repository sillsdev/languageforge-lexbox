import { expect } from '@playwright/test';
import { test } from './fixtures';
import { loginAs } from './utils/authHelpers';
import * as testEnv from './envVars';
import { AdminDashboardPage } from './pages/adminDashboardPage';
import { ProjectPage } from './pages/projectPage';
import { ResetProjectModal } from './components/resetProjectModal';

test('reset project and upload .zip file', async ({ page, tempProject }) => {
  // Step 1: Populate project with known initial state
  await loginAs(page.request, 'admin', testEnv.defaultPassword);
  const adminDashboardPage = await new AdminDashboardPage(page).goto();
  await adminDashboardPage.clickProject(tempProject.name);
  const projectPage = await new ProjectPage(page, tempProject.name, tempProject.code).waitFor();
  await projectPage.clickResetProject();
  const resetProjectModel = await new ResetProjectModal(page).waitFor();
  await resetProjectModel.clickNextStepButton('I have a working backup');
  await resetProjectModel.confirmProjectBackupReceived(tempProject.code);
  await resetProjectModel.clickNextStepButton('Reset project');
  await resetProjectModel.uploadProjectZipFile('tests/data/test-project-one-commit.zip');
  await expect(page.getByText('Project successfully reset')).toBeVisible();
  await page.getByRole('button', { name: 'Close' }).click();
  await resetProjectModel.assertGone();

  // Step 2: Get tip hash and file list from hgweb, check some known values
  await page.goto(`${testEnv.serverBaseUrl}/hg/${tempProject.code}/file/tip`);
  await expect(page.locator('tr.fileline')).toHaveCount(1);
  await expect(page.locator('tr.fileline').first()).toHaveText(/hello\.txt/);
  const h3BeforeReset = await page.locator('.main h3').innerText();

  // Step 3: reset project, do not upload zip file
  await projectPage.goto();
  await projectPage.clickResetProject();
  await resetProjectModel.waitFor();
  const download = await resetProjectModel.downloadProjectBackup();
  // TODO: Create fixture to use temporary directory and tear it down after test, as of right now this pollutes current working directory
  await download.saveAs('tests/data/reset-project-test-step-1.zip');
  await resetProjectModel.clickNextStepButton('I have a working backup');
  await resetProjectModel.confirmProjectBackupReceived(tempProject.code);
  await resetProjectModel.clickNextStepButton('Reset project');
  await resetProjectModel.clickNextStepButton('Leave project empty');
  await page.getByRole('button', { name: 'Close' }).click();
  await resetProjectModel.assertGone();

  // Step 4: confirm it's empty now
  await page.goto(`${testEnv.serverBaseUrl}/hg/${tempProject.code}/file/tip`);
  await expect(page.locator('tr.fileline')).toHaveCount(0);
  await expect(page.locator('.main h3')).not.toHaveText(h3BeforeReset);

  // Step 5: reset project again, uploading zip file downloaded from step 1
  await projectPage.goto();
  await projectPage.clickResetProject();
  await resetProjectModel.waitFor();
  await resetProjectModel.clickNextStepButton('I have a working backup');
  await resetProjectModel.confirmProjectBackupReceived(tempProject.code);
  await resetProjectModel.clickNextStepButton('Reset project');
  await resetProjectModel.uploadProjectZipFile('tests/data/reset-project-test-step-1.zip');
  await expect(page.getByText('Project successfully reset')).toBeVisible();
  await page.getByRole('button', { name: 'Close' }).click();
  await resetProjectModel.assertGone();

  // Step 6: confirm hash in <h3> element is same as before reset
  await page.goto(`${testEnv.serverBaseUrl}/hg/${tempProject.code}/file/tip`);
  await expect(page.locator('tr.fileline')).toHaveCount(1);
  await expect(page.locator('tr.fileline').first()).toHaveText(/hello\.txt/);
  await expect(page.locator('.main h3')).toHaveText(h3BeforeReset);
});
