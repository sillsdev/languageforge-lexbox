import { test, expect } from '@playwright/test';
import { loginAs } from './authHelpers';
import * as testEnv from './envVars';
import { AdminDashboardPage } from './pages/adminDashboardPage';
import { ProjectPage } from './pages/projectPage';
import { ResetProjectModal } from './components/resetProjectModal';

test('reset project and upload .zip file', async ({ page }) => {
  await loginAs(page.request, 'admin', testEnv.defaultPassword);
  const adminDashboardPage = await new AdminDashboardPage(page).goto();
  await adminDashboardPage.clickProject('Sena 3');
  const projectPage = await new ProjectPage(page, 'Sena 3', 'sena-3').waitFor();
  await projectPage.clickResetProject();
  const resetProjectModel = await new ResetProjectModal(page).waitFor();
  const download = await resetProjectModel.downloadProjectBackup();
  await download.saveAs('sena-3.zip'); // TODO: Create fixture to use temporary directory and tear it down after test, as of right now this pollutes current working directory
  await resetProjectModel.clickNextStepButton('I have a working backup');
  await resetProjectModel.confirmProjectBackupReceived('sena-3');
  await resetProjectModel.clickNextStepButton('Reset project');
  await resetProjectModel.uploadProjectZipFile('sena-3.zip');
  // Should go to next step automatically if successful, no clickNextStepButton needed this time
  await expect(page.getByText('Project successfully reset')).toBeVisible();
  await page.getByRole('button', { name: 'Close' }).click();
  await resetProjectModel.assertGone();
});
