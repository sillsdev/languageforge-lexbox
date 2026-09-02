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

  test('menu offers vernacular writing systems and excludes audio', async () => {
    const {page} = projectPage;
    await sortTrigger().click();

    // The default vernacular keeps the historical "Headword" label; other vernacular
    // writing systems are listed by name (the demo has Sena phonetic and Chichewa).
    await expect(page.getByRole('menuitem', {name: 'Headword'}).first()).toBeVisible();
    await expect(page.getByRole('menuitem', {name: 'Chichewa'}).first()).toBeVisible();
    // Audio writing systems are excluded.
    await expect(page.getByRole('menuitem', {name: 'Sena Audio'})).toHaveCount(0);
  });

  test('sorting by a writing system switches the displayed headword to that ws', async () => {
    const {page} = projectPage;

    // The served demo data is almost entirely single-writing-system, so create an entry with
    // distinct Sena (seh) and Chichewa (ny) forms that share a searchable token.
    await projectPage.api.createEntryWithForms({seh: 'qtxseh', ny: 'qtxny'});

    await projectPage.entriesList.filterByText('qtx');
    const headword = projectPage.entriesList.entryRows
      .filter({hasNotText: 'Add to dictionary'})
      .locator('h2');

    // Under the default sort the headword is the default vernacular (Sena) form.
    await expect(headword).toHaveText('qtxseh');

    // Switch the sort to Chichewa (ny), ascending.
    await sortTrigger().click();
    await page.getByRole('menuitem', {name: 'Chichewa'}).first().click();

    // The trigger reflects the new sort, and the headword now shows the Chichewa form.
    await expect(page.getByRole('button', {name: 'Chichewa'})).toBeVisible();
    await expect(headword).toHaveText('qtxny');
  });
});
