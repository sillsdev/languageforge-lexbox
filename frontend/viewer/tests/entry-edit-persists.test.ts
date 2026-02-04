import {expect, test} from '@playwright/test';
import {BrowsePage} from './browse-page';

/**
 * Critical tests: Verify that entry edits are saved to the backend.
 *
 * Note: The demo environment uses in-memory storage that resets on page reload.
 * We use existing demo entries and verify changes via API immediately after UI edits.
 * This catches the most common failure mode: UI appearing to save but not calling the API.
 */
test.describe('Entry edit persistence', () => {

  test('UI edit of gloss field is saved to backend', async ({page}) => {
    const browsePage = new BrowsePage(page);
    await browsePage.goto();

    // Get an existing entry with senses from demo data that has an English gloss
    const {entryId, headword, originalGloss} = await browsePage.api.getEntryWithEnglishGloss();
    expect(entryId).toBeTruthy();
    expect(headword).toBeTruthy();
    expect(originalGloss).toBeTruthy();

    // Select the entry
    await browsePage.selectEntryByFilter(headword);

    // Verify we have the expected original value
    const glossInput = await browsePage.entryView.getGlossInput(0, 'Eng');
    await expect(glossInput).toHaveValue(originalGloss);

    // Edit the gloss
    const timestamp = Date.now().toString().slice(-6);
    const newGloss = `edited-${timestamp}`;
    await browsePage.entryView.editGloss(newGloss, 0, 'Eng');

    // Verify via API that the change was saved
    await expect(async () => {
      const savedGloss = await browsePage.api.getEntryGloss(entryId, 'en');
      expect(savedGloss).toBe(newGloss);
    }).toPass({timeout: 5000});

    // Also verify the UI shows the new value
    await expect(glossInput).toHaveValue(newGloss);
  });

  test('adding sense via UI is saved to backend', async ({page}) => {
    const browsePage = new BrowsePage(page);
    await browsePage.goto();

    // Get an existing entry and count its senses
    const {entryId, headword, senseCount: initialSenseCount} = await browsePage.api.getEntryAtIndex(15);
    expect(entryId).toBeTruthy();
    expect(headword).toBeTruthy();

    // Select the entry
    await browsePage.selectEntryByFilter(headword);

    // Add a new sense
    await browsePage.entryView.addSense();

    // Verify UI shows new sense
    const senseCount = await browsePage.entryView.getSenseCount();
    expect(senseCount).toBeGreaterThan(initialSenseCount);

    // Fill in the new sense's gloss
    const timestamp = Date.now().toString().slice(-6);
    const senseGloss = `new-sense-${timestamp}`;
    await browsePage.entryView.editGloss(senseGloss, senseCount - 1);

    // Verify via API that the sense was added
    await expect(async () => {
      const savedSenseCount = await browsePage.api.getEntrySenseCount(entryId);
      expect(savedSenseCount).toBe(initialSenseCount + 1);
      const hasGloss = await browsePage.api.entryHasGlossValue(entryId, senseGloss);
      expect(hasGloss).toBe(true);
    }).toPass({timeout: 5000});

    // Also verify the UI shows the new sense with the gloss we entered
    const newGlossInput = await browsePage.entryView.getGlossInput(senseCount - 1);
    await expect(newGlossInput).toHaveValue(senseGloss);
  });

  test('editing lexeme form is saved to backend', async ({page}) => {
    const browsePage = new BrowsePage(page);
    await browsePage.goto();

    // Get an existing entry from the demo data
    const {entryId, headword: originalLexeme} = await browsePage.api.getEntryAtIndex(10);
    expect(entryId).toBeTruthy();
    expect(originalLexeme).toBeTruthy();

    // Search and select the entry
    await browsePage.selectEntryByFilter(originalLexeme);

    // Edit the lexeme
    const timestamp = Date.now().toString().slice(-6);
    const editMarker = `-E${timestamp}`;
    const newLexeme = originalLexeme + editMarker;

    await browsePage.entryView.editLexemeForm(newLexeme);

    // Verify via API that the change was saved
    await expect(async () => {
      const savedLexeme = await browsePage.api.getEntryLexeme(entryId);
      expect(savedLexeme).toBe(newLexeme);
    }).toPass({timeout: 5000});

    // Also verify the UI shows the new value
    const lexemeInput = await browsePage.entryView.getLexemeInput();
    await expect(lexemeInput).toHaveValue(newLexeme);
  });
});
