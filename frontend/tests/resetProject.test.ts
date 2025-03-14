import * as testEnv from './envVars';

import {AdminDashboardPage} from './pages/adminDashboardPage';
import {ProjectPage} from './pages/projectPage';
import {expect} from '@playwright/test';
import {join} from 'path';
import {loginAs} from './utils/authHelpers';
import {test, type TempProject} from './fixtures';
import {delay} from '$lib/util/time';

type HgWebFileJson = {
  abspath: string
  basename: string
}

type HgWebJson = {
  node: string
  files: HgWebFileJson[]
}

test('reset project and upload .zip file', async ({page, tempProject, tempDir}) => {
  test.slow();

  const allZeroHash = '0000000000000000000000000000000000000000';

  // Step 1: Populate project with known initial state
  await loginAs(page.request, 'admin');
  const adminDashboardPage = await new AdminDashboardPage(page).goto();
  await adminDashboardPage.clickProject(tempProject.name);
  const projectPage = await new ProjectPage(page, tempProject.name, tempProject.code).waitFor();
  const resetProjectModel = await projectPage.clickResetProject();
  await resetProjectModel.clickNextStepButton('I have a working backup');
  await resetProjectModel.confirmProjectBackupReceived(tempProject.code);
  await resetProjectModel.clickNextStepButton('Reset project');
  await waitUntilProjectIsOldEnoughToReturnRealChangesets(tempProject);
  await resetProjectModel.uploadProjectZipFile('tests/data/test-project-one-commit.zip');
  await expect(page.getByText('Project successfully reset')).toBeVisible();
  await page.getByRole('button', { name: 'Close' }).click();
  await resetProjectModel.assertGone();

  // Step 2: Get tip hash and file list from hgweb, check some known values
  const beforeResetResponse = await page.request.get(`${testEnv.serverBaseUrl}/hg/${tempProject.code}/file/tip?style=json-lex`);
  const beforeResetJson = await beforeResetResponse.json() as HgWebJson;
  expect(beforeResetJson).toHaveProperty('node');
  expect(beforeResetJson.node).not.toEqual(allZeroHash);
  const linkToHgWebTip = page.locator(`a[href="/hg/${tempProject.code}/file/${beforeResetJson.node}"]`);
  await expect(linkToHgWebTip).toBeVisible();
  expect(beforeResetJson).toHaveProperty('files');
  expect(beforeResetJson.files).toHaveLength(1);
  expect(beforeResetJson.files[0]).toHaveProperty('basename');
  expect(beforeResetJson.files[0].basename).toBe('hello.txt');

  // Step 3: reset project, do not upload zip file
  await projectPage.clickResetProject();
  const download = await resetProjectModel.downloadProjectBackup();
  await download.saveAs(join(tempDir, 'reset-project-test-step-1.zip'));
  await resetProjectModel.clickNextStepButton('I have a working backup');
  await resetProjectModel.confirmProjectBackupReceived(tempProject.code);
  await resetProjectModel.clickNextStepButton('Reset project');
  await resetProjectModel.clickNextStepButton('Leave project empty');
  await page.getByRole('button', { name: 'Close' }).click();
  await resetProjectModel.assertGone();

  // Step 4: confirm it's empty now
  const afterResetResponse = await page.request.get(`${testEnv.serverBaseUrl}/hg/${tempProject.code}/file/tip?style=json-lex`);
  const afterResetJson = await afterResetResponse.json() as HgWebJson;
  expect(afterResetJson.node).toEqual(allZeroHash);
  await expect(linkToHgWebTip).not.toBeVisible();
  expect(afterResetJson).toHaveProperty('files');
  expect(afterResetJson.files).toHaveLength(0);

  // Step 5: reset project again, uploading zip file downloaded from step 1
  await projectPage.clickResetProject();
  await resetProjectModel.clickNextStepButton('I have a working backup');
  await resetProjectModel.confirmProjectBackupReceived(tempProject.code);
  await resetProjectModel.clickNextStepButton('Reset project');
  await resetProjectModel.uploadProjectZipFile(join(tempDir, 'reset-project-test-step-1.zip'));
  await expect(page.getByText('Project successfully reset')).toBeVisible();
  await page.getByRole('button', { name: 'Close' }).click();
  await resetProjectModel.assertGone();

  // Step 6: confirm tip hash and contents are same as before reset
  const afterUploadResponse = await page.request.get(`${testEnv.serverBaseUrl}/hg/${tempProject.code}/file/tip?style=json-lex`);
  const afterResetJSon = await afterUploadResponse.json() as HgWebJson;
  expect(afterResetJSon).toEqual(beforeResetJson); // NOT .toBe(), which would check that they're the same object.
  // and the UI has updated
  await expect(linkToHgWebTip).toBeVisible();
});

async function waitUntilProjectIsOldEnoughToReturnRealChangesets(tempProject: TempProject): Promise<void> {
  const projectAge = Date.now() - tempProject.createdAt;
  const requiredAge = 6_000; // see C#'s Project.GetChangesets
  const grace = 2_000;
  if (projectAge < requiredAge) {
    const waitTime = requiredAge - projectAge;
    await delay(waitTime + grace);
  }
}
