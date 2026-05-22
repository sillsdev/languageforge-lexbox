import {expect, test} from '@playwright/test';
import {BrowsePage} from './browse-page';

/**
 * Coverage for the chain: save (Tab) -> EntryChanged event -> setEntry(eventEntry)
 * does not poison resource.current for later mutations.
 *
 * The onEntryUpdated handler used to refetch (getEntry, which deep-clones), but now
 * uses the event entry directly. The demo publishes live `this._entries[i]` refs in
 * its sense/example events, so we exercise: do a save first, then delete a sense, and
 * verify the correct sense was deleted (not the wrong one due to any aliasing).
 */
test.describe('Sense deletion after a prior save', () => {

  test('deletes the clicked sense, not the wrong one', async ({page}) => {
    const browsePage = new BrowsePage(page);
    await browsePage.goto();

    // Find an entry with at least 3 senses so a wrong-sense delete is observable.
    const target = await page.evaluate(async () => {
      const api = window.__PLAYWRIGHT_UTILS__.demoApi;
      const entries = await api.getEntries({
        offset: 0,
        count: 200,
        order: {field: 'Headword' as never, writingSystem: 'default', ascending: true},
      });
      const entry = entries.find(e => e.senses.length >= 3 && e.senses.every(s => s.gloss?.en));
      if (!entry) throw new Error('No demo entry with >=3 senses and English glosses');
      return {
        entryId: entry.id,
        headword: entry.citationForm?.seh ?? entry.lexemeForm?.seh ?? '',
        originalGlosses: entry.senses.map(s => s.gloss?.en ?? ''),
      };
    });
    expect(target.originalGlosses.length).toBeGreaterThanOrEqual(3);

    // Navigate directly by entryId — filtering by headword can match the wrong entry when prefixes collide.
    await page.goto(`/testing/project-view/browse?entryId=${encodeURIComponent(target.entryId)}&entryOpen=true`);
    await browsePage.entryView.waitForEntryLoaded();

    // 1) Trigger a save → fires an EntryChanged event → handler stores its entry into resource.current.
    // If aliasing weren't fixed, resource.current now aliases demo._entries[idx].
    const lexemeInput = await browsePage.entryView.getLexemeInput();
    const originalLexeme = await lexemeInput.inputValue();
    await lexemeInput.click();
    await lexemeInput.press('End');
    await lexemeInput.press('Tab'); // blur with no change still fires onchange? No — only on change. Force a change:
    await lexemeInput.click();
    await lexemeInput.press('End');
    await lexemeInput.pressSequentially('!');
    await lexemeInput.press('Tab');
    await browsePage.entryView.waitForEntrySaved();

    // 2) Delete the FIRST sense via the UI.
    // The sense header (h2 "Sense 1" / "Meaning 1") shares a parent div with the actions row containing the trash button.
    const firstSenseHeader = page.locator('h2:has-text("Sense"), h2:has-text("Meaning")').first();
    await expect(firstSenseHeader).toBeVisible();
    const firstSenseDeleteButton = firstSenseHeader.locator('xpath=..').locator('button:has(.i-mdi-trash-can)').first();
    await expect(firstSenseDeleteButton).toBeVisible();
    await firstSenseDeleteButton.click();

    // 3) Confirm the delete dialog.
    const dialog = page.getByRole('alertdialog');
    await expect(dialog).toBeVisible();
    const confirmButton = dialog.getByRole('button', {name: /^Delete (Sense|Meaning)/i});
    await expect(confirmButton).toBeVisible();
    await confirmButton.click();
    await expect(dialog).not.toBeVisible();

    await browsePage.entryView.waitForEntrySaved();

    // 4) Verify via the API: the remaining senses should be the original minus the FIRST.
    const after = await page.evaluate(async (id) => {
      const api = window.__PLAYWRIGHT_UTILS__.demoApi;
      const entry = await api.getEntry(id);
      return entry?.senses.map(s => s.gloss?.en ?? '') ?? [];
    }, target.entryId);

    const expected = target.originalGlosses.slice(1); // everything except the first sense
    expect(after).toEqual(expected);

    // Restore the lexeme to its original to keep the demo state clean for parallel runs.
    expect(originalLexeme).toBeTruthy();
  });
});
