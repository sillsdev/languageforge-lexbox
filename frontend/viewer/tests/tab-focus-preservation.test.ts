import {expect, test} from '@playwright/test';

import {BrowsePage} from './browse-page';

/**
 * When the user types in a field and presses Tab, the save must not mess with the focus.
 */
test.describe('Tab focus preservation', () => {

  test('Tab from edited lexeme moves focus to a different element and keeps it', async ({page}) => {
    const browsePage = new BrowsePage(page);
    await browsePage.goto();

    const {entryId, headword} = await browsePage.api.getEntryAtIndex(10);
    expect(entryId).toBeTruthy();

    await browsePage.selectEntryByFilter(headword);

    const lexemeInput = await browsePage.entryView.getLexemeInput();
    await expect(lexemeInput).toBeVisible();

    await lexemeInput.click();
    await lexemeInput.press('ControlOrMeta+a');
    const timestamp = Date.now().toString().slice(-6);
    await lexemeInput.fill(`tab-focus-${timestamp}`);

    // Make sure the lexeme input is what's focused right before we Tab.
    await expect(lexemeInput).toBeFocused();

    await lexemeInput.press('Tab');

    // Wait long enough that any debounced save + EntryChanged event would have fired
    // and previously would have stolen focus by calling editor.commit() -> blur.
    await browsePage.entryView.waitForEntrySaved();

    // Focus must be on something tabbable other than the lexeme input, and not on body.
    await expect(lexemeInput).not.toBeFocused();
    const focusedTag = await page.evaluate(() => document.activeElement?.tagName ?? null);
    expect(focusedTag).not.toBe('BODY');
    expect(focusedTag).not.toBeNull();
  });
});
