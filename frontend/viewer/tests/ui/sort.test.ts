import {expect, test} from '@playwright/test';

import {DemoProjectPage} from './demo-project.page';

test.describe('Sort by writing system', () => {
  let projectPage: DemoProjectPage;

  test.beforeEach(async ({page}) => {
    projectPage = new DemoProjectPage(page);
    await projectPage.goto();
  });

  function sortTrigger() {
    return projectPage.page.getByTestId('sort-menu-trigger');
  }
  function wsTrigger() {
    return projectPage.page.getByTestId('sort-ws-trigger');
  }

  test('sort menu keeps only headword/relevance; writing systems live in the pill and exclude audio', async () => {
    const {page} = projectPage;

    // The sort menu no longer lists writing systems (that moved to its own pill).
    await sortTrigger().click();
    await expect(page.getByRole('menuitem', {name: 'Headword'}).first()).toBeVisible();
    await expect(page.getByRole('menuitem', {name: 'Chichewa'})).toHaveCount(0);
    await page.keyboard.press('Escape');

    // The writing-system pill lists the vernacular writing systems, excluding audio.
    await wsTrigger().click();
    await expect(page.getByRole('menuitem', {name: 'Chichewa'})).toBeVisible();
    await expect(page.getByRole('menuitem', {name: 'Sena Audio'})).toHaveCount(0);
  });

  test('selecting a writing system switches the displayed headword to that ws', async () => {
    const {page} = projectPage;

    // The served demo data is almost entirely single-writing-system, so create an entry with
    // distinct Sena (seh) and Chichewa (ny) forms that share a searchable token.
    await projectPage.api.createEntryWithForms({seh: 'qtxseh', ny: 'qtxny'});

    await projectPage.entriesList.filterByText('qtx');
    const headword = projectPage.entriesList.entryRows
      .filter({hasNotText: 'Add to dictionary'})
      .locator('h2');

    // By default the headword is the default vernacular (Sena) form.
    await expect(headword).toHaveText('qtxseh');

    // Switch the writing system to Chichewa (ny) via the pill.
    await wsTrigger().click();
    await page.getByRole('menuitem', {name: 'Chichewa'}).click();

    // The headword now shows the Chichewa form.
    await expect(headword).toHaveText('qtxny');
  });
});
