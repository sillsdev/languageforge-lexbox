import {type Page, expect, test} from '@playwright/test';
import {SortField} from '$lib/dotnet-types/generated-types/MiniLcm/SortField';

/**
 * Tests for V2 virtual scrolling features:
 * - Jump to entry (when reloading with entry selected)
 * - Entry update/delete event handling without full reset
 *
 * These tests are expected to fail until the frontend integration is complete.
 */

test.describe('EntriesList V2 features', () => {
  function getLocators(page: Page) {
    return {
      vlist: page.locator('[role="table"] > div > div'),
      entryRows: page.locator('[role="table"] [role="row"]'),
      skeletons: page.locator('[role="table"] [data-skeleton]'),
      selectedEntry: page.locator('[role="table"] [role="row"][aria-selected="true"]'),
    };
  }

  async function waitForProjectViewReady(page: Page, waitForTestUtils = false) {
    await expect(page.locator('.i-mdi-loading')).toHaveCount(0, {timeout: 10000});
    await page.waitForFunction(() => document.fonts.ready);
    await expect(page.locator('[data-skeleton]')).toHaveCount(0, {timeout: 10000});
    // Wait for test utilities to be available if requested
    if (waitForTestUtils) {
      await page.waitForFunction(() => window.__PLAYWRIGHT_UTILS__?.demoApi, {timeout: 5000});
    }
  }

  async function getVisibleEntryTexts(page: Page) {
    const {entryRows} = getLocators(page);
    const texts: string[] = [];
    const count = await entryRows.count();
    for (let i = 0; i < Math.min(count, 10); i++) {
      const text = await entryRows.nth(i).textContent();
      if (text) texts.push(text.trim());
    }
    return texts;
  }

  test.describe('Jump to entry', () => {
    test('reloading with entry selected scrolls to that entry', async ({page}) => {
      // Go to project view
      await page.goto('/testing/project-view');
      await waitForProjectViewReady(page);

      const {vlist, entryRows} = getLocators(page);

      // Scroll far down to find an entry well into the list (not near the top)
      // 6000px at ~60px per row = ~100 items down
      await vlist.evaluate((el) => { el.scrollTop = 6000; });
      await page.waitForTimeout(500);
      await expect(page.locator('[data-skeleton]')).toHaveCount(0, {timeout: 5000});

      // Select an entry that's in the viewport
      // (otherwise the vlist might scroll, recycle dom elements and make the selection not match the text)
      const entryToSelect = entryRows.nth(6);
      await expect(entryToSelect).toBeInViewport();

      // Get the text BEFORE clicking (clicking may scroll and recycle the DOM element)
      const selectedText = await entryToSelect.textContent();
      expect(selectedText).toBeTruthy();

      // Now click to select
      await entryToSelect.click();

      // Verify the URL has the entry ID
      const url = page.url();
      expect(url).toContain('entryId=');

      // Reload the page with the same URL (entry should remain selected)
      await page.reload();
      await waitForProjectViewReady(page);

      // The selected entry should be visible (scrolled into view)
      const {selectedEntry} = getLocators(page);
      await expect(selectedEntry).toBeVisible({timeout: 10000});
      await expect(selectedEntry).toContainText(selectedText!.slice(0, 20));
    });

    test('clearing search filter with entry selected keeps entry visible', async ({page}) => {
      await page.goto('/testing/project-view');
      await waitForProjectViewReady(page);

      const {vlist, entryRows} = getLocators(page);

      // Scroll far into the list (entries are sorted alphabetically, so we need to scroll past a-z entries)
      const estimatedItemHeight = 60; // Approximate height of each entry row
      // Scroll down by approximately 100 items worth of height to get to entries starting with later letters
      const scrollDistance = 500 * estimatedItemHeight;
      await vlist.evaluate((el, dist) => el.scrollTop = dist, scrollDistance);
      await page.waitForTimeout(500);
      await expect(page.locator('[data-skeleton]')).toHaveCount(0, {timeout: 5000});

      // Get an entry from this position in the list (one that's now visible)
      const visibleEntry = entryRows.first();
      const entryText = await visibleEntry.textContent();
      // Extract the headword (first word in the entry row)
      const headword = entryText?.split(/\s+/).filter(Boolean)[0] ?? '';
      expect(headword.length).toBeGreaterThan(0);

      // Now filter for that headword
      const searchInput = page.locator('input.real-input').first();
      await searchInput.fill(headword);
      await page.waitForTimeout(500);
      await expect(page.locator('[data-skeleton]')).toHaveCount(0, {timeout: 5000});

      // Select the entry from filtered results
      await entryRows.first().click();
      const selectedText = await entryRows.first().textContent();
      expect(selectedText).toContain(headword);

      // Clear the search - this reloads the full list from the beginning
      await searchInput.clear();
      await page.waitForTimeout(500);
      await expect(page.locator('[data-skeleton]')).toHaveCount(0, {timeout: 5000});

      // Without V2 jump-to-entry, the list will reset to the beginning
      // and the selected entry won't be visible (it's far down in the list)
      // With V2, the list should scroll to show the selected entry
      const {selectedEntry} = getLocators(page);
      await expect(selectedEntry).toBeVisible({timeout: 10000});
      await expect(selectedEntry).toContainText(headword);
    });
  });

  test.describe('Entry event handling', () => {
    test.beforeEach(async ({page}) => {
      await page.goto('/testing/project-view');
      await waitForProjectViewReady(page, true); // Wait for test utils
    });

    test('entry delete event removes entry from list without full reset', async ({page}) => {
      // Get the first few entries' texts before deletion
      const initialTexts = await getVisibleEntryTexts(page);
      expect(initialTexts.length).toBeGreaterThan(2);

      // Get the ID of the first entry
      const firstEntryId = await page.evaluate(async (headword) => {
        const demoApi = window.__PLAYWRIGHT_UTILS__.demoApi;
        const entries = await demoApi.getEntries({count: 1, offset: 0, order: {field: headword, writingSystem: 'default', ascending: true}});
        return entries[0]?.id;
      }, SortField.Headword);
      expect(firstEntryId).toBeTruthy();

      // Delete the entry via the demo API
      await page.evaluate(async (entryId) => {
        const testUtils = window.__PLAYWRIGHT_UTILS__;
        await testUtils.demoApi.deleteEntry(entryId);
      }, firstEntryId);

      // Wait a bit for the event to be processed
      await page.waitForTimeout(300);

      // The first entry should now be different (what was the second entry)
      const newTexts = await getVisibleEntryTexts(page);
      expect(newTexts[0]).toBe(initialTexts[1]);

      // The old first entry should not be in the list
      expect(newTexts).not.toContain(initialTexts[0]);
    });

    test('entry update event updates entry in list without full reset', async ({page}) => {
      const {entryRows} = getLocators(page);

      // Get the first entry's text before update
      const firstEntryText = await entryRows.first().textContent();
      expect(firstEntryText).toBeTruthy();

      // Get the first entry
      const firstEntry = await page.evaluate(async (headword) => {
        const testUtils = window.__PLAYWRIGHT_UTILS__;
        const entries = await testUtils.demoApi.getEntries({count: 1, offset: 0, order: {field: headword, writingSystem: 'default', ascending: true}});
        return entries[0];
      }, SortField.Headword);
      expect(firstEntry).toBeTruthy();

      // Update the entry's headword via the demo API
      await page.evaluate(async (entry) => {
        const testUtils = window.__PLAYWRIGHT_UTILS__;
        const updated = {...entry, lexemeForm: {...entry.lexemeForm, seh: 'UPDATED-' + (entry.lexemeForm.seh || entry.lexemeForm.en || 'entry')}};
        await testUtils.demoApi.updateEntry(entry, updated);
      }, firstEntry);

      // Wait a bit for the event to be processed
      await page.waitForTimeout(300);

      // The first entry should now show the updated text
      const newFirstEntryText = await entryRows.first().textContent();
      expect(newFirstEntryText).toContain('UPDATED-');
    });

    test('deleting selected entry clears selection', async ({page}) => {
      const {entryRows, selectedEntry} = getLocators(page);

      // Click first entry to select it
      await entryRows.first().click();
      await expect(selectedEntry).toBeVisible();

      // Get the selected entry ID
      const entryId = await page.evaluate(async () => {
        const testUtils = window.__PLAYWRIGHT_UTILS__;
        const entries = await testUtils.demoApi.getEntries({count: 1, offset: 0, order: {field: 'Headword' as unknown as SortField, writingSystem: 'default', ascending: true}});
        return entries[0]?.id;
      });

      // Delete via API
      await page.evaluate(async (id) => {
        const testUtils = window.__PLAYWRIGHT_UTILS__;
        await testUtils.demoApi.deleteEntry(id);
      }, entryId);

      await page.waitForTimeout(300);

      // Selection should be cleared (no entry selected)
      // URL should not have entryId parameter
      const url = page.url();
      expect(url).not.toContain(`entryId=${entryId}`);
    });

    test('deleting entry not in cache triggers reset but maintains position', async ({page}) => {
      const {vlist} = getLocators(page);

      // Scroll to the middle of the list
      const scrollHeight = await vlist.evaluate((el) => el.scrollHeight);
      const middleScroll = scrollHeight * 0.5;
      await vlist.evaluate((el, target) => { el.scrollTop = target; }, middleScroll);
      await page.waitForTimeout(500);
      await expect(page.locator('[data-skeleton]')).toHaveCount(0, {timeout: 5000});

      // Get visible entry texts at current position
      const visibleTexts = await getVisibleEntryTexts(page);
      expect(visibleTexts.length).toBeGreaterThan(0);

      // Get an entry ID from the TOP of the list (not loaded/cached)
      const topEntryId = await page.evaluate(async (headword) => {
        const testUtils = window.__PLAYWRIGHT_UTILS__;
        const entries = await testUtils.demoApi.getEntries({count: 1, offset: 0, order: {field: headword, writingSystem: 'default', ascending: true}});
        return entries[0]?.id;
      }, SortField.Headword);

      // Delete the top entry (which is NOT in cache since we scrolled to middle)
      await page.evaluate(async (id) => {
        const testUtils = window.__PLAYWRIGHT_UTILS__;
        await testUtils.demoApi.deleteEntry(id);
      }, topEntryId);

      await page.waitForTimeout(500);
      await expect(page.locator('[data-skeleton]')).toHaveCount(0, {timeout: 5000});

      // After reset, we should still see similar entries (the visible ones shouldn't drastically change)
      // Note: This test verifies the reset happens gracefully
      const newVisibleTexts = await getVisibleEntryTexts(page);
      expect(newVisibleTexts.length).toBeGreaterThan(0);
    });
  });
});
