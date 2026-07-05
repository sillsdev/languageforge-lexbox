import {expect, test, type Page} from '@playwright/test';

import {EntriesListComponent} from './entries-list-component';
import {EntryApiHelper} from './entry-api-helper';

async function selectSortOption(page: Page, currentLabel: RegExp, optionLabel: RegExp): Promise<void> {
  await page.getByRole('button', {name: currentLabel}).click();
  // both directions of a field share the label; the ascending item comes first
  await page.getByRole('menuitem', {name: optionLabel}).first().click();
}

test.describe('Sort by meaning (gloss)', () => {
  let entriesList: EntriesListComponent;
  let api: EntryApiHelper;

  test.beforeEach(async ({page}) => {
    api = new EntryApiHelper(page);
    entriesList = new EntriesListComponent(page, api);
    await entriesList.goto();
  });

  test('shows one row per sense in gloss order and can switch back', async ({page}) => {
    const entryCount = await api.countEntries();
    const senseRowCount = await api.countSenseRows();
    expect(senseRowCount).toBeGreaterThan(entryCount);

    await selectSortOption(page, /Headword/, /Meaning|Gloss/);

    const expectedRows = await api.getSenseRows(0, 10);
    await entriesList.waitForSkeletonsToResolve();

    // visible rows follow the expected gloss order (headword still leads each row)
    for (let i = 0; i < 5; i++) {
      const expected = expectedRows[i];
      await expect(entriesList.entryRows.nth(i)).toContainText(expected.headword);
      if (expected.gloss) {
        await expect(entriesList.entryRows.nth(i)).toContainText(expected.gloss);
      }
    }

    // a multi-sense entry occupies one row per sense, within the rows asserted above
    const assertedRows = expectedRows.slice(0, 5);
    const duplicated = assertedRows.find((row, _, all) =>
      all.filter(other => other.entryId === row.entryId).length > 1);
    expect(duplicated, 'demo data should surface a multi-sense entry near the top').toBeDefined();

    // switching back to headword sort restores one row per entry
    await selectSortOption(page, /Meaning|Gloss/, /Headword/);
    await entriesList.waitForSkeletonsToResolve();
    const firstHeadword = await api.getHeadwordAtIndex(0);
    await expect(entriesList.entryRows.first()).toContainText(firstHeadword);
  });

  test('selecting a row opens its entry', async ({page}) => {
    await selectSortOption(page, /Headword/, /Meaning|Gloss/);
    await entriesList.waitForSkeletonsToResolve();

    const [firstRow] = await api.getSenseRows(0, 1);
    await entriesList.entryRows.first().click();

    await expect(entriesList.selectedEntry).toContainText(firstRow.headword);
    await expect(page).toHaveURL(new RegExp(`entryId=${firstRow.entryId}`));
  });
});
