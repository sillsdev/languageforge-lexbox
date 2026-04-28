import {expect, test} from '@playwright/test';
import {BrowsePage} from './browse-page';

test.describe('Back navigation persistence', () => {

  test('edit is applied to correct entry when navigating back without blurring', async ({page}) => {
    const browsePage = new BrowsePage(page);
    await browsePage.goto();

    // Get three entries from demo data to ensure a solid history stack
    const entryAId = await browsePage.api.getEntryIdAtIndex(0);
    const entryBId = await browsePage.api.getEntryIdAtIndex(1);
    const entryCId = await browsePage.api.getEntryIdAtIndex(2);

    const originalLexemeA = await browsePage.api.getEntryLexeme(entryAId);
    const originalLexemeB = await browsePage.api.getEntryLexeme(entryBId);

    // 1) Open entry A
    await browsePage.entriesList.selectEntryByIndex(0);
    await browsePage.entryView.waitForEntryLoaded();

    // 2) Open entry B
    await browsePage.entriesList.selectEntryByIndex(1);
    await browsePage.entryView.waitForEntryLoaded();

    // 3) Open entry C
    await browsePage.entriesList.selectEntryByIndex(2);
    await browsePage.entryView.waitForEntryLoaded();

    // 4) Edit a field on entry C (lexeme form)
    const lexemeInput = await browsePage.entryView.getLexemeInput();
    const timestamp = Date.now().toString().slice(-6);
    const newValueC = `edited-C-${timestamp}`;

    await lexemeInput.click();
    await lexemeInput.press('ControlOrMeta+a');
    await lexemeInput.fill(newValueC);
    // DO NOT press Tab or Blur!

    // 5) Trigger a back navigation WITHOUT blurring first
    await page.goBack();

    // Wait for entry B to be loaded again in the UI
    await expect(page.locator('.i-mdi-loading')).toHaveCount(0, {timeout: 10000});
    await browsePage.entryView.waitForEntryLoaded();

    // Ensure the input value for Entry B is NOT newValueC
    const lexemeInputAfterNav = await browsePage.entryView.getLexemeInput();
    await expect(lexemeInputAfterNav).not.toHaveValue(newValueC);
    await expect(lexemeInputAfterNav).toHaveValue(originalLexemeB);

    // 6) Verify via API that entry B and A were NOT changed
    const savedLexemeB = await browsePage.api.getEntryLexeme(entryBId);
    expect(savedLexemeB, 'Entry B should not have the edit meant for Entry C').toBe(originalLexemeB);
    const savedLexemeA = await browsePage.api.getEntryLexeme(entryAId);
    expect(savedLexemeA, 'Entry A should not have the edit meant for Entry C').toBe(originalLexemeA);

    // 7) Verify via API that entry C WAS changed
    const savedLexemeC = await browsePage.api.getEntryLexeme(entryCId);
    expect(savedLexemeC).toBe(newValueC);
  });
});
