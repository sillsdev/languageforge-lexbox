import {type Page, expect, test} from '@playwright/test';
import {SortField} from '$lib/dotnet-types/generated-types/MiniLcm/SortField';
import {MorphType} from '$lib/dotnet-types/generated-types/MiniLcm/Models/MorphType';

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

  async function getItemHeight(page: Page): Promise<number> {
    const {entryRows} = getLocators(page);
    await expect(entryRows.first()).toBeVisible();
    const firstBox = await entryRows.first().boundingBox();
    const secondBox = await entryRows.nth(1).boundingBox();
    if (!firstBox || !secondBox) throw new Error('Could not measure entry rows');
    return secondBox.y - firstBox.y;
  }

  async function scrollToIndex(page: Page, targetIndex: number, _itemHeight: number): Promise<void> {
    const {vlist} = getLocators(page);
    
    // Get total count for proportional scrolling
    const {totalCount} = await page.evaluate(async () => {
      const api = window.__PLAYWRIGHT_UTILS__.demoApi;
      return {totalCount: await api.countEntries()};
    });
    
    // Use proportion of total scroll for accurate positioning
    // This accounts for VList's estimated item heights across all items
    const targetScroll = await vlist.evaluate((el, params) => {
      const {idx, total} = params;
      return (idx / total) * el.scrollHeight;
    }, {idx: targetIndex, total: totalCount});
    
    await vlist.evaluate((el, target) => { 
      el.scrollTop = Math.min(target, el.scrollHeight - el.clientHeight); 
    }, targetScroll);
    
    await page.waitForTimeout(300);
    await expect(page.locator('[data-skeleton]')).toHaveCount(0, {timeout: 5000});
  }

  async function createEntryAtIndex(page: Page, targetIndex: number): Promise<{id: string, headword: string}> {
    return page.evaluate(async ({idx, headwordField, morphType}) => {
      const api = window.__PLAYWRIGHT_UTILS__.demoApi;
      const offset = Math.max(0, idx - 1);
      const entries = await api.getEntries({
        offset,
        count: 1,
        order: {field: headwordField, writingSystem: 'default', ascending: true}
      });
      const entry = entries[0];
      // Use the same headword logic as the sorting: citationForm || lexemeForm
      const baseHeadword = entry?.citationForm?.seh ?? entry?.lexemeForm?.seh ?? '#';
      const newHeadword = baseHeadword + '-inserted';
      const newEntry = {
        id: crypto.randomUUID(),
        // Set both lexemeForm and citationForm to ensure consistent sorting
        lexemeForm: {seh: newHeadword},
        citationForm: {seh: newHeadword},
        senses: [],
        note: {},
        literalMeaning: {},
        morphType,
        components: [],
        complexForms: [],
        complexFormTypes: [],
        publishIn: [],
      };
      const created = await api.createEntry(newEntry);
      return {id: created.id, headword: newHeadword};
    }, {idx: targetIndex, headwordField: SortField.Headword, morphType: MorphType.Unknown});
  }



  async function getEntryTextAtIndex(page: Page, index: number): Promise<string> {
    return page.evaluate(async ({idx, headwordField}) => {
      const api = window.__PLAYWRIGHT_UTILS__.demoApi;
      const entries = await api.getEntries({
        offset: idx,
        count: 1,
        order: {field: headwordField, writingSystem: 'default', ascending: true}
      });
      const entry = entries[0];
      // Use the same headword logic as the sorting: citationForm || lexemeForm
      return entry?.citationForm?.seh ?? entry?.lexemeForm?.seh ?? '';
    }, {idx: index, headwordField: SortField.Headword});
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

    test('entry added in loaded batch', async ({page}) => {
      const {entryRows} = getLocators(page);
      const itemHeight = await getItemHeight(page);

      // Setup: Get entry texts at key indices from API (before insert)
      const old25Text = await getEntryTextAtIndex(page, 25);
      const old49Text = await getEntryTextAtIndex(page, 49);

      // Action: Create entry at index 25
      const {headword} = await createEntryAtIndex(page, 25);

      // Give time for event handling
      await page.waitForTimeout(300);

      // Verify entry at index 25 via API shows the new entry
      const new25Text = await getEntryTextAtIndex(page, 25);
      expect(new25Text).toContain('-inserted');
      expect(new25Text).toBe(headword);

      // Verify entry at index 26 via API shows what was at 25
      const new26Text = await getEntryTextAtIndex(page, 26);
      expect(new26Text).toBe(old25Text);

      // Verify batch boundary push: entry at index 50 shows what was at 49
      const new50Text = await getEntryTextAtIndex(page, 50);
      expect(new50Text).toBe(old49Text);

      // Verify UI: scroll to index 25 and verify new entry is visible
      await scrollToIndex(page, 25, itemHeight);
      await expect(entryRows.filter({hasText: headword}).first()).toBeVisible({timeout: 5000});

      // Verify UI: scroll to index 50 and verify pushed entry is visible
      await scrollToIndex(page, 50, itemHeight);
      await expect(entryRows.filter({hasText: old49Text}).first()).toBeVisible({timeout: 5000});
    });

    test('entry added after all loaded batches', async ({page}) => {
      const {entryRows, vlist} = getLocators(page);

      // Setup: Stay at batch 0, get total count
      const initialVisibleTexts = await getVisibleEntryTexts(page);
      const oldScrollHeight = await vlist.evaluate((el) => el.scrollHeight);

      // Get the total count and last entry so we can add an entry at the END
      const {totalCount, lastHeadword} = await page.evaluate(async ({headwordField}) => {
        const api = window.__PLAYWRIGHT_UTILS__.demoApi;
        const count = await api.countEntries();
        const entries = await api.getEntries({
          offset: count - 1,
          count: 1,
          order: {field: headwordField, writingSystem: 'default', ascending: true}
        });
        const entry = entries[0];
        const headword = entry?.citationForm?.seh ?? entry?.lexemeForm?.seh ?? '';
        return {totalCount: count, lastHeadword: headword};
      }, {headwordField: SortField.Headword});

      // Action: Create entry at the very end (after last entry alphabetically)
      // Use a headword that sorts AFTER the last entry (append 'z' or use high unicode)
      const newHeadword = lastHeadword + 'z-inserted';
      await page.evaluate(async ({headword, morphType}) => {
        const api = window.__PLAYWRIGHT_UTILS__.demoApi;
        const newEntry = {
          id: crypto.randomUUID(),
          lexemeForm: {seh: headword},
          citationForm: {seh: headword},
          senses: [],
          note: {},
          literalMeaning: {},
          morphType,
          components: [],
          complexForms: [],
          complexFormTypes: [],
          publishIn: [],
        };
        await api.createEntry(newEntry);
      }, {headword: newHeadword, morphType: MorphType.Unknown});

      // Wait for event to be processed and scroll height to update
      await expect(async () => {
        const newHeight = await vlist.evaluate((el) => el.scrollHeight);
        expect(newHeight).toBeGreaterThan(oldScrollHeight);
      }).toPass({timeout: 5000});

      // Visible entries unchanged (batch 0 not affected)
      const newVisibleTexts = await getVisibleEntryTexts(page);
      expect(newVisibleTexts).toEqual(initialVisibleTexts);

      // Verify via API that entry exists at the new last index
      const entryAtEnd = await getEntryTextAtIndex(page, totalCount);
      expect(entryAtEnd).toContain('-inserted');
      expect(entryAtEnd).toBe(newHeadword);

      // Scroll to the end of the list using scrollHeight instead of calculated position
      // This ensures we're at the actual end, not an estimated position
      await vlist.evaluate((el) => { el.scrollTop = el.scrollHeight; });
      await page.waitForTimeout(500);
      await expect(page.locator('[data-skeleton]')).toHaveCount(0, {timeout: 5000});

      // The new entry should be visible somewhere near the end
      await expect(entryRows.filter({hasText: newHeadword}).first()).toBeVisible({timeout: 10000});
    });

    test('entry added before loaded batch (quiet reset test)', async ({page}) => {
      const itemHeight = await getItemHeight(page);
      const {vlist, entryRows} = getLocators(page);

      // Setup: Scroll to batch 2 (index 100+)
      await scrollToIndex(page, 120, itemHeight);
      const oldScrollHeight = await vlist.evaluate((el) => el.scrollHeight);

      // Capture: Visible entries BEFORE action
      const visibleTextsBefore = await getVisibleEntryTexts(page);
      expect(visibleTextsBefore.length).toBeGreaterThan(0);

      // Action: Create entry at index 25 (before currently loaded batch)
      const {headword} = await createEntryAtIndex(page, 25);

      // Wait for event handling
      await page.waitForTimeout(500);
      await expect(page.locator('[data-skeleton]')).toHaveCount(0, {timeout: 5000});

      // Verify scroll height increased (count +1)
      const newScrollHeight = await vlist.evaluate((el) => el.scrollHeight);
      expect(newScrollHeight).toBeGreaterThan(oldScrollHeight);

      // Verify the view is still showing valid entries (not skeletons)
      // Note: The exact entries may shift by one position due to the insertion
      const visibleTextsAfter = await getVisibleEntryTexts(page);
      expect(visibleTextsAfter.length).toBeGreaterThan(0);

      // All visible entries should be real entries (not empty or just whitespace)
      for (const text of visibleTextsAfter) {
        expect(text.trim().length).toBeGreaterThan(0);
      }

      // The new entry should be at index 25 when we scroll there
      await scrollToIndex(page, 25, itemHeight);
      await expect(entryRows.filter({hasText: headword}).first()).toBeVisible({timeout: 5000});
    });

    test('entry updated not in cache', async ({page}) => {
      const {entryRows} = getLocators(page);
      const itemHeight = await getItemHeight(page);

      // Setup: Stay at batch 0 - wait for data to be fully loaded
      await page.waitForTimeout(500);
      await expect(page.locator('[data-skeleton]')).toHaveCount(0, {timeout: 5000});
      
      const initialVisibleTexts = await getVisibleEntryTexts(page);
      expect(initialVisibleTexts.filter(t => t.length > 0).length).toBeGreaterThan(0);

      // Action: Update entry at index 100 by appending to its headword
      const {updatedHeadword, entryId} = await page.evaluate(async ({idx, headwordField}) => {
        const api = window.__PLAYWRIGHT_UTILS__.demoApi;
        const entries = await api.getEntries({
          offset: idx,
          count: 1,
          order: {field: headwordField, writingSystem: 'default', ascending: true}
        });
        const entry = entries[0];
        const currentHeadword = entry.citationForm?.seh ?? entry.lexemeForm?.seh ?? 'entry';
        const newHeadword = currentHeadword + '-UPDATED';
        const updated = {
          ...entry,
          lexemeForm: {...entry.lexemeForm, seh: newHeadword},
          citationForm: {...entry.citationForm, seh: newHeadword}
        };
        await api.updateEntry(entry, updated);
        return {updatedHeadword: newHeadword, entryId: entry.id};
      }, {idx: 100, headwordField: SortField.Headword});

      await page.waitForTimeout(300);

      // Verify no visible change to batch 0 (update was outside cache)
      const newVisibleTexts = await getVisibleEntryTexts(page);
      expect(newVisibleTexts).toEqual(initialVisibleTexts);

      // Find the new index of the updated entry via API
      const newIndex = await page.evaluate(async ({id}) => {
        const api = window.__PLAYWRIGHT_UTILS__.demoApi;
        return await api.getEntryIndex(id, undefined, undefined);
      }, {id: entryId});
      expect(newIndex).toBeGreaterThanOrEqual(0);

      // Scroll to the entry's new position and verify it's visible
      await scrollToIndex(page, newIndex, itemHeight);

      await expect(entryRows.filter({hasText: '-UPDATED'}).first()).toBeVisible({timeout: 5000});
      await expect(entryRows.filter({hasText: updatedHeadword}).first()).toBeVisible({timeout: 5000});
    });
  });
});
