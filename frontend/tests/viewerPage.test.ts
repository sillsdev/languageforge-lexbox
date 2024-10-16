import * as testEnv from './envVars';

import { UserDashboardPage } from './pages/userDashboardPage';
import { ViewerPage } from './pages/viewerPage';
import { expect } from '@playwright/test';
import { loginAs } from './utils/authHelpers';
import { test } from './fixtures';

test('navigate to viewer', async ({ page }) => {
  // Step 1: Login
  await loginAs(page.request, 'editor', testEnv.defaultPassword);
  const userDashboard = await new UserDashboardPage(page).goto();

  // Step 2: Click through to viewer
  const projectPage = await userDashboard.openProject('Sena 3', 'sena-3');
  const viewerPage = await projectPage.clickBrowseInViewer();
  await viewerPage.dismissAboutDialog();
});

test('find entry', async ({ page }) => {
  // Step 1: Login to viewer
  await loginAs(page.request, 'editor', testEnv.defaultPassword);
  const viewerPage = await new ViewerPage(page, 'Sena 3', 'sena-3').goto();
  await viewerPage.dismissAboutDialog();

  // Step 2: Use search bar to go to entry "shrew"
  await viewerPage.search('animal');
  await expect(viewerPage.searchResults).toHaveCount(5);
  await viewerPage.clickSearchResult('shrew');

  async function verifyViewerState(): Promise<void> {
    // Step 3: Check that we are on the shrew entry
    await expect(viewerPage.entryDictionaryPreview).toContainText('shrew');
    await expect(viewerPage.page.locator('.field:has-text("Citation form") input:visible'))
      .toHaveValue('nkhwizi');

    // Step 4: Verify entry list filter and order
    await expect(viewerPage.entryListItems).toHaveCount(7);
    await expect(viewerPage.entryListItems.nth(0)).toContainText('cifuwo');
    await expect(viewerPage.entryListItems.nth(3)).toContainText('shrew');
    await expect(viewerPage.entryListItems.nth(6)).toContainText('nyama');
  }

  await verifyViewerState();
  await viewerPage.page.reload();
  await viewerPage.waitFor();
  await verifyViewerState();
});

test('entry details', async ({ page }) => {
  // Step 1: Login to viewer at entry "thembe"
  await loginAs(page.request, 'editor', testEnv.defaultPassword);
  const viewerPage = await new ViewerPage(page, 'Sena 3', 'sena-3')
    .goto({ urlEnd: '?entryId=49cc9257-90c7-4fe0-a9e0-2c8d72aa5e2b&search=animal' });
  await viewerPage.dismissAboutDialog();

  // Step 2: verify entry details
  // -- Dictionary Preview
  const expectPreview = expect(viewerPage.entryDictionaryPreview);
  await expectPreview.toContainText('nthembe');
  await expectPreview.toContainText(' N. ');
  await expectPreview.toContainText(' Eng ');
  await expectPreview.toContainText('animal skin alone, after it is taken off the body');
  await expectPreview.toContainText(' Por ');
  await expectPreview.toContainText('pele de animal');

  // -- Entry fields
  const lexeme = viewerPage.page.locator('.field:has-text("Lexeme") .ws-field-wrapper:visible');
  await expect(lexeme.getByRole('textbox')).toHaveValue('thembe');
  await expect(lexeme.getByLabel('Sen', { exact: true })).toHaveValue('thembe');

  await expect(viewerPage.page.locator('.field:has-text("Citation form") input:visible')).toHaveValue('nthembe');

  // -- Sense
  await expect(viewerPage.page.locator('[id^="sense"]')).toHaveCount(1);
  await expect(viewerPage.page.locator(':text("gloss")')).toHaveCount(1);

  const gloss = viewerPage.page.locator('.field:has-text("Gloss") .ws-field-wrapper:visible');
  await expect(gloss).toHaveCount(2);
  await expect(gloss.getByLabel('Eng')).toHaveValue('animal skin');
  await expect(gloss.getByLabel('Por')).toHaveValue('pele de animal');

  const definition = viewerPage.page.locator('.field:has-text("Definition") .ws-field-wrapper:visible');
  await expect(definition).toHaveCount(1);
  await expect(definition.getByLabel('Eng')).toHaveValue('animal skin alone, after it is taken off the body');

  const grammaticalInfo = viewerPage.page.locator('.field:has-text("Grammatical info") input:visible');
  await expect(grammaticalInfo).toHaveValue('Nome');

  const semanticDomains = viewerPage.page.locator('.field:has-text("Semantic domain") input:visible');
  await expect(semanticDomains).toHaveValue('1.6.2 Parts of an animal');

  // -- Example
  await expect(viewerPage.page.locator('[id^="example"]')).toHaveCount(1);
  const reference = viewerPage.page.locator('.field:has-text("Reference") input');
  await expect(reference).toHaveValue('Wordlist');
});
