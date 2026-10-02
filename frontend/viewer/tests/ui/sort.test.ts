import {expect, test} from '@playwright/test';

import {DemoProjectPage} from './demo-project.page';
import {ViewBase} from '$lib/dotnet-types/generated-types/MiniLcm/Models/ViewBase';

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

  test('defaults to the first vernacular writing system of the current view', async () => {
    const {page} = projectPage;

    await projectPage.api.createEntryWithForms({seh: 'qtxseh', ny: 'qtxny'});
    await projectPage.entriesList.filterByText('qtx');
    const headword = projectPage.entriesList.entryRows
      .filter({hasNotText: 'Add to dictionary'})
      .locator('h2');
    await expect(headword).toHaveText('qtxseh');
    await expect(wsTrigger()).toBeVisible();

    // A custom view that shows Chichewa as its only non-audio vernacular writing system.
    await page.evaluate((base) => window.__PLAYWRIGHT_UTILS__.addCustomView({
      id: crypto.randomUUID(),
      name: 'Chichewa only',
      base,
      entryFields: [{fieldId: 'lexemeForm'}],
      senseFields: [{fieldId: 'gloss'}],
      exampleFields: [{fieldId: 'sentence'}],
      vernacular: [{wsId: 'ny'}],
    }), ViewBase.FwLite);
    await page.getByRole('button').filter({has: page.locator('.i-mdi-layers')}).click();
    await page.getByRole('radio', {name: 'Chichewa only (Lite)'}).click();
    await expect(page.getByRole('radio', {name: 'Chichewa only (Lite)'})).toBeChecked();
    await page.keyboard.press('Escape');

    // The headword follows the view's only vernacular, so there's nothing left to pick in the pill.
    await expect(headword).toHaveText('qtxny');
    await expect(wsTrigger()).toHaveCount(0);
  });

  test('a view with only audio vernaculars falls back to every text vernacular', async () => {
    const {page} = projectPage;

    await projectPage.api.createEntryWithForms({seh: 'qtxseh', ny: 'qtxny'});
    await projectPage.entriesList.filterByText('qtx');
    const row = projectPage.entriesList.entryRows.filter({hasNotText: 'Add to dictionary'});

    // The demo's "Portuguese and audio" view only shows the Sena audio vernacular.
    await page.getByRole('button').filter({has: page.locator('.i-mdi-layers')}).click();
    await page.getByRole('tab', {name: 'Preview'}).click();
    await page.getByRole('button').filter({has: page.locator('.i-mdi-layers')}).click();
    await page.getByRole('radio', {name: 'Portuguese and audio (Lite)'}).click();
    await expect(page.getByRole('radio', {name: 'Portuguese and audio (Lite)'})).toBeChecked();
    await page.keyboard.press('Escape');

    // The preview still shows headwords, sorted by and switchable between the text vernaculars.
    await expect(row.locator('strong')).toHaveText('qtxseh / qtxny');
    await expect(wsTrigger()).toBeVisible();
  });
});
