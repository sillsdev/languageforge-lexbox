import {expect, test, type Locator} from '@playwright/test';

import {DemoProjectPage} from './demo-project.page';
import {ViewBase} from '$lib/dotnet-types/generated-types/MiniLcm/Models/ViewBase';

/** Add Meaning / Add Example dialogs: what blocks a submit and what discards the draft. Saving is covered in entry-edit-persists. */
test.describe('Add sense/example dialog', () => {
  let projectPage: DemoProjectPage;
  let entryId: string;
  let initialSenseCount: number;

  test.beforeEach(async ({page}) => {
    projectPage = new DemoProjectPage(page);
    await projectPage.goto();
    const entry = await projectPage.api.getEntryAtIndex(15);
    entryId = entry.entryId;
    initialSenseCount = entry.senseCount;
    await projectPage.selectEntryByFilter(entry.headword);
  });

  async function expectNoSenseAdded(): Promise<void> {
    expect(await projectPage.entryView.getSenseCount()).toBe(initialSenseCount);
    await projectPage.entryView.waitForEntrySaved();
    expect(await projectPage.api.getEntrySenseCount(entryId)).toBe(initialSenseCount);
  }

  test('adding an empty sense or example is blocked with a message', async ({page}) => {
    await projectPage.entryView.addSenseButton.click();
    const dialog = page.getByRole('dialog');
    await dialog.getByRole('button', {name: /add (sense|meaning)/i}).click();
    await expect(dialog.getByText('Gloss or Definition is required')).toBeVisible();
    await dialog.getByRole('button', {name: 'Cancel'}).click();
    await expect(dialog).toBeHidden();

    await page.getByRole('button', {name: 'Add Example'}).first().click();
    await dialog.getByRole('button', {name: 'Add Example'}).click();
    await expect(dialog.getByText('Sentence is required')).toBeVisible();

    await expectNoSenseAdded();
  });

  test('a view without gloss or definition still needs some field filled in', async ({page}) => {
    await page.evaluate((base) => window.__PLAYWRIGHT_UTILS__.addCustomView({
      id: crypto.randomUUID(),
      name: 'Grammar only',
      base,
      entryFields: [{fieldId: 'lexemeForm'}],
      senseFields: [{fieldId: 'partOfSpeechId'}],
      exampleFields: [{fieldId: 'sentence'}],
    }), ViewBase.FwLite);
    await page.getByRole('button').filter({has: page.locator('.i-mdi-layers')}).first().click();
    await page.getByRole('radio', {name: 'Grammar only (Lite)'}).click();
    await page.keyboard.press('Escape');

    await projectPage.entryView.addSenseButton.click();
    const dialog = page.getByRole('dialog');
    await expect(dialog.locator('[data-field-id="gloss"]')).toBeHidden();
    await dialog.getByRole('button', {name: /add (sense|meaning)/i}).click();
    await expect(dialog.getByText('Fill in at least one field')).toBeVisible();
    await dialog.getByRole('button', {name: 'Cancel'}).click();
    await expect(dialog).toBeHidden();

    await expectNoSenseAdded();
  });

  const cancelPaths: Record<string, (dialog: Locator) => Promise<void>> = {
    'clicking Cancel': (dialog) => dialog.getByRole('button', {name: 'Cancel'}).click(),
    // the gloss input still has focus, so it commits while the dialog closes
    'pressing Escape': (dialog) => dialog.page().keyboard.press('Escape'),
    'pressing Enter on Cancel': async (dialog) => {
      await dialog.getByRole('button', {name: 'Cancel'}).focus();
      await dialog.page().keyboard.press('Enter');
    },
  };
  for (const [path, cancel] of Object.entries(cancelPaths)) {
    test(`${path} discards the draft sense`, async ({page}) => {
      const pageErrors: Error[] = [];
      page.on('pageerror', error => pageErrors.push(error));

      await projectPage.entryView.addSenseButton.click();
      const dialog = page.getByRole('dialog');
      await expect(dialog).toBeVisible();
      const glossInput = dialog.locator('[data-field-id="gloss"] input').first();
      await glossInput.fill('discarded-sense');
      await expect(glossInput).toHaveValue('discarded-sense');
      await cancel(dialog);
      await expect(dialog).toBeHidden();

      await expectNoSenseAdded();
      expect(pageErrors).toEqual([]);
    });
  }
});
