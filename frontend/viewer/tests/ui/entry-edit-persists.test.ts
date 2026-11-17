import {expect, test} from '@playwright/test';

import {DemoProjectPage} from './demo-project.page';

/**
 * Critical tests: Verify that entry edits are saved to the backend.
 *
 * Note: The demo environment uses in-memory storage that resets on page reload.
 * We use existing demo entries and verify changes via API immediately after UI edits.
 * This catches the most common failure mode: UI appearing to save but not calling the API.
 */
test.describe('Entry edit persistence', () => {

  test('UI edit of gloss field is saved to backend', async ({page}) => {
    const projectPage = new DemoProjectPage(page);
    await projectPage.goto();

    // Get an existing entry with senses from demo data that has an English gloss
    const {entryId, headword, originalGloss} = await projectPage.api.getEntryWithEnglishGloss();
    expect(entryId).toBeTruthy();
    expect(headword).toBeTruthy();
    expect(originalGloss).toBeTruthy();

    // Select the entry
    await projectPage.selectEntryByFilter(headword);

    // Verify we have the expected original value
    const glossInput = await projectPage.entryView.getGlossInput(0, 'Eng');
    await expect(glossInput).toHaveValue(originalGloss);

    // Edit the gloss
    const timestamp = Date.now().toString().slice(-6);
    const newGloss = `edited-${timestamp}`;
    await projectPage.entryView.editGloss(newGloss, 0, 'Eng');

    // Verify via API that the change was saved
    await expect(async () => {
      const savedGloss = await projectPage.api.getEntryGloss(entryId, 'en');
      expect(savedGloss).toBe(newGloss);
    }).toPass({timeout: 5000});

    // Also verify the UI shows the new value
    await expect(glossInput).toHaveValue(newGloss);
  });

  test('adding sense via UI is saved to backend', async ({page}) => {
    const projectPage = new DemoProjectPage(page);
    await projectPage.goto();

    // Get an existing entry and count its senses
    const {entryId, headword, senseCount: initialSenseCount} = await projectPage.api.getEntryAtIndex(15);
    expect(entryId).toBeTruthy();
    expect(headword).toBeTruthy();

    // Select the entry
    await projectPage.selectEntryByFilter(headword);

    // Add a new sense via the dialog
    const timestamp = Date.now().toString().slice(-6);
    const senseGloss = `new-sense-${timestamp}`;
    await projectPage.entryView.addSense(senseGloss);

    // Verify UI shows new sense
    const senseCount = await projectPage.entryView.getSenseCount();
    expect(senseCount).toBe(initialSenseCount + 1);

    // Verify via API that the sense was added
    await expect(async () => {
      const savedSenseCount = await projectPage.api.getEntrySenseCount(entryId);
      expect(savedSenseCount).toBe(initialSenseCount + 1);
      const hasGloss = await projectPage.api.entryHasGlossValue(entryId, senseGloss);
      expect(hasGloss).toBe(true);
    }).toPass({timeout: 5000});

    // Also verify the UI shows the new sense with the gloss we entered
    const newGlossInput = await projectPage.entryView.getGlossInput(senseCount - 1);
    await expect(newGlossInput).toHaveValue(senseGloss);
  });

  test('adding example via dialog is saved to backend', async ({page}) => {
    const projectPage = new DemoProjectPage(page);
    await projectPage.goto();

    const {entryId, headword} = await projectPage.api.getEntryWithEnglishGloss();
    async function exampleCount(): Promise<number> {
      const entry = await projectPage.api.getEntry(entryId) as {senses: {exampleSentences: unknown[]}[]};
      return entry.senses[0].exampleSentences.length;
    }
    const initialExampleCount = await exampleCount();
    await projectPage.selectEntryByFilter(headword);

    await page.getByRole('button', {name: 'Add Example'}).first().click();
    const dialog = page.getByRole('dialog');
    await expect(dialog).toBeVisible();
    const sentence = `new-example-${Date.now().toString().slice(-6)}`;
    const sentenceEditor = dialog.locator('[data-field-id="sentence"] [contenteditable="true"]').first();
    await sentenceEditor.click();
    await sentenceEditor.pressSequentially(sentence);
    await dialog.getByRole('button', {name: 'Add Example'}).click();
    await expect(dialog).toBeHidden();

    await expect.poll(exampleCount, {timeout: 5000}).toBe(initialExampleCount + 1);
    expect(JSON.stringify(await projectPage.api.getEntry(entryId))).toContain(sentence);
    await expect(page.locator('[data-field-id="sentence"]').filter({hasText: sentence})).toBeVisible();
  });

  test('editing lexeme form is saved to backend', async ({page}) => {
    const projectPage = new DemoProjectPage(page);
    await projectPage.goto();

    // Get an existing entry from the demo data
    const {entryId, headword: originalLexeme} = await projectPage.api.getEntryAtIndex(10);
    expect(entryId).toBeTruthy();
    expect(originalLexeme).toBeTruthy();

    // Search and select the entry
    await projectPage.selectEntryByFilter(originalLexeme);

    // Edit the lexeme
    const timestamp = Date.now().toString().slice(-6);
    const editMarker = `-E${timestamp}`;
    const newLexeme = originalLexeme + editMarker;

    await projectPage.entryView.editLexemeForm(newLexeme);

    // Verify via API that the change was saved
    await expect(async () => {
      const savedLexeme = await projectPage.api.getEntryLexeme(entryId);
      expect(savedLexeme).toBe(newLexeme);
    }).toPass({timeout: 5000});

    // Also verify the UI shows the new value
    const lexemeInput = await projectPage.entryView.getLexemeInput();
    await expect(lexemeInput).toHaveValue(newLexeme);
  });
});
