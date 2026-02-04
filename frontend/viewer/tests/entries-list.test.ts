import {expect, test} from '@playwright/test';
import {EntriesListComponent} from './entries-list-component';
import {EntryApiHelper} from './entry-api-helper';

const ESTIMATED_ITEM_HEIGHT = 60;
const BATCH_SIZE = 50;

test.describe('EntriesList', () => {
  let entriesList: EntriesListComponent;
  let api: EntryApiHelper;

  test.describe('Lazy loading', () => {
    test.beforeEach(async ({page}) => {
      api = new EntryApiHelper(page);
      entriesList = new EntriesListComponent(page, api);
      await entriesList.goto();
    });

    test('entries are loaded initially', async () => {
      await expect(entriesList.entryRows.first()).toBeVisible();

      const visibleCount = await entriesList.entryRows.count();
      expect(visibleCount).toBeGreaterThan(5);

      await expect(entriesList.entryRows.first()).not.toHaveAttribute('data-skeleton');
      await expect(entriesList.entryRows.first()).toContainText(/.+/);
    });

    test('can scroll through entries incrementally', async () => {
      const initialTexts = await entriesList.getVisibleEntryTexts(5);
      expect(initialTexts.length).toBeGreaterThan(0);

      await entriesList.scrollToPixels(1000);

      await expect(async () => {
        const scrollTop = await entriesList.getScrollTop();
        expect(scrollTop).toBeGreaterThan(850);
      }).toPass({timeout: 2000});

      const newTexts = await entriesList.getVisibleEntryTexts(5);
      expect(newTexts).not.toEqual(initialTexts);

      await expect(entriesList.skeletons).toHaveCount(0);
    });

    test('large scroll jump loads new entries and preserves unloaded entries', async () => {
      await expect(entriesList.entryRows.first()).toBeVisible();

      const scrollHeight = await entriesList.getScrollHeight();
      expect(scrollHeight).toBeGreaterThan(500);

      // Jump to 90% of list
      const targetScroll = scrollHeight * 0.9;
      await entriesList.scrollToPixels(targetScroll);

      await expect(async () => {
        const scrollTop = await entriesList.getScrollTop();
        expect(scrollTop).toBeGreaterThan(targetScroll - 200);
      }).toPass({timeout: 2000});

      // Should resolve from skeletons to entries
      await expect(async () => {
        await expect(entriesList.entryRows.first()).toBeVisible();
        const skeletonCount = await entriesList.skeletons.count();
        expect(skeletonCount).toBeLessThan(3);
      }).toPass({timeout: 5000});

      // Scroll back to middle (should show skeletons then load)
      await entriesList.scrollToPercent(0.5);

      // Middle was not loaded yet - verify skeletons appear briefly
      await entriesList.page.waitForTimeout(100);
      await expect(entriesList.skeletons.first()).toBeVisible();

      // Eventually loads content
      await expect(async () => {
        await expect(entriesList.entryRows.first()).toBeVisible();
        await expect(entriesList.skeletons).toHaveCount(0);
      }).toPass({timeout: 5000});

      await expect(entriesList.entryRows.first()).not.toHaveAttribute('data-skeleton');
    });
  });

  test.describe('Jump to entry', () => {
    test.beforeEach(async ({page}) => {
      api = new EntryApiHelper(page);
      entriesList = new EntriesListComponent(page, api);
      await entriesList.goto();
    });

    test('reloading with entry selected scrolls to that entry', async ({page}) => {
      // Scroll far down (~100 items)
      await entriesList.scrollToPixels(100 * ESTIMATED_ITEM_HEIGHT);
      await entriesList.page.waitForTimeout(200);
      await entriesList.waitForSkeletonsToResolve();

      // Select an entry that's visible (get text before click to avoid DOM recycling issues)
      const selectedText = await entriesList.selectEntryByIndex(6);
      expect(selectedText).toBeTruthy();

      expect(page.url()).toContain('entryId=');

      // Reload and verify selected entry is still visible
      await page.reload();
      await entriesList.waitForSkeletonsToResolve();

      await expect(entriesList.selectedEntry).toBeVisible({timeout: 5000});
      const expectedSnippet = selectedText.slice(0, 20);
      await expect(entriesList.selectedEntry).toContainText(expectedSnippet);
    });

    test('clearing search filter with entry selected keeps entry visible', async () => {
      // Scroll far into the list (~500 items down)
      await entriesList.scrollToPixels(500 * ESTIMATED_ITEM_HEIGHT);
      await entriesList.page.waitForTimeout(200);
      await entriesList.waitForSkeletonsToResolve();

      // Get headword from visible entry
      const entryText = await entriesList.entryRows.first().textContent();
      const headword = entryText?.split(/\s+/).filter(Boolean)[0] ?? '';
      expect(headword.length).toBeGreaterThan(0);

      // Filter and select
      await entriesList.filterByText(headword);
      await entriesList.entryRows.first().click();
      await expect(entriesList.entryRows.first()).toContainText(headword);

      // Clear filter - selected entry should still be visible
      await entriesList.clearFilter();

      await expect(entriesList.selectedEntry).toBeVisible({timeout: 10000});
      await expect(entriesList.selectedEntry).toContainText(headword);
    });
  });

  test.describe('Entry event handling', () => {
    test.beforeEach(async ({page}) => {
      api = new EntryApiHelper(page);
      entriesList = new EntriesListComponent(page, api);
      await entriesList.goto(true);
    });

    test('entry delete event removes entry from list without full reset', async () => {
      const initialTexts = await entriesList.getVisibleEntryTexts();
      expect(initialTexts.length).toBeGreaterThan(2);

      const firstEntryId = await api.getEntryIdAtIndex(0);
      await api.deleteEntry(firstEntryId);

      await expect(async () => {
        const newTexts = await entriesList.getVisibleEntryTexts();
        expect(newTexts[0]).toBe(initialTexts[1]);
        expect(newTexts).not.toContain(initialTexts[0]);
      }).toPass({timeout: 5000});
    });

    test('entry update event updates entry in list without full reset', async () => {
      const firstEntryText = await entriesList.entryRows.first().textContent();
      expect(firstEntryText).toBeTruthy();

      // Update first entry by prepending to its headword (so it stays at index 0)
      const {updatedHeadword} = await api.updateEntryHeadwordPrepend(0, '-UPDATED-');

      // The first entry in UI should now show the updated text
      await expect(async () => {
        const newFirstEntryText = await entriesList.entryRows.first().textContent();
        expect(newFirstEntryText).toContain('-UPDATED-');
      }).toPass({timeout: 5000});

      await expect(entriesList.entryWithText(updatedHeadword)).toBeVisible();
    });

    test('deleting selected entry clears selection', async () => {
      await entriesList.entryRows.first().click();
      await expect(entriesList.selectedEntry).toBeVisible();

      const entryId = await api.getEntryIdAtIndex(0);
      await api.deleteEntry(entryId);

      await entriesList.page.waitForTimeout(300);
      await expect(entriesList.selectedEntry).not.toBeAttached();
    });

    test('deleting entry not in cache triggers reset but maintains position', async () => {
      // Scroll to middle
      await entriesList.scrollToPercent(0.5);
      await entriesList.waitForSkeletonsToResolve();

      const visibleTexts = await entriesList.getVisibleEntryTexts();
      expect(visibleTexts.length).toBeGreaterThan(0);

      // Delete entry from top (not in cache)
      const topEntryId = await api.getEntryIdAtIndex(0);
      await api.deleteEntry(topEntryId);

      await entriesList.page.waitForTimeout(500);
      await entriesList.waitForSkeletonsToResolve();

      // Should still have visible entries after reset
      const newVisibleTexts = await entriesList.getVisibleEntryTexts();
      expect(newVisibleTexts.length).toBeGreaterThan(0);
    });

    test('adding entry in loaded batch updates UI in place', async () => {
      const midBatchIndex = Math.floor(BATCH_SIZE / 2);
      const lastIndexInBatch0 = BATCH_SIZE - 1;

      const oldMidText = await api.getHeadwordAtIndex(midBatchIndex);
      const oldLastText = await api.getHeadwordAtIndex(lastIndexInBatch0);

      const {headword} = await api.createEntryAtIndex(midBatchIndex);
      await entriesList.page.waitForTimeout(300);

      // Verify API reflects the insertion
      const newMidText = await api.getHeadwordAtIndex(midBatchIndex);
      expect(newMidText).toBe(headword);

      const shiftedMidText = await api.getHeadwordAtIndex(midBatchIndex + 1);
      expect(shiftedMidText).toBe(oldMidText);

      const shiftedLastText = await api.getHeadwordAtIndex(lastIndexInBatch0 + 1);
      expect(shiftedLastText).toBe(oldLastText);

      // Verify UI shows the new entry
      await entriesList.scrollToIndex(midBatchIndex);
      await expect(entriesList.entryWithText(headword)).toBeVisible({timeout: 5000});

      await entriesList.scrollToIndex(lastIndexInBatch0 + 1);
      await expect(entriesList.entryWithText(oldLastText)).toBeVisible({timeout: 5000});
    });

    test('adding entry at end increases scroll height without affecting visible entries', async () => {
      const initialVisibleTexts = await entriesList.getVisibleEntryTexts();
      const oldScrollHeight = await entriesList.getScrollHeight();

      const {headword: lastHeadword, index: lastIndex} = await api.getLastEntry();
      const newHeadword = lastHeadword + 'z-inserted';
      await api.createEntryWithHeadword(newHeadword);

      // Wait for scroll height to increase
      await expect(async () => {
        const newHeight = await entriesList.getScrollHeight();
        expect(newHeight).toBeGreaterThan(oldScrollHeight);
      }).toPass({timeout: 5000});

      // Visible entries unchanged (batch 0 not affected)
      const newVisibleTexts = await entriesList.getVisibleEntryTexts();
      expect(newVisibleTexts).toEqual(initialVisibleTexts);

      // Verify via API
      const entryAtEnd = await api.getHeadwordAtIndex(lastIndex + 1);
      expect(entryAtEnd).toBe(newHeadword);

      // Scroll to end and verify visible
      await entriesList.scrollToEnd();
      await expect(entriesList.entryWithText(newHeadword)).toBeVisible({timeout: 10000});
    });

    test('entry added before loaded batch triggers quiet reset', async () => {
      const midBatchIndex = Math.floor(BATCH_SIZE / 2);
      const indexInBatch2 = BATCH_SIZE * 2 + 20;
      const entriesToAdd = 10;

      // Scroll to batch 2
      await entriesList.scrollToIndex(indexInBatch2);
      const oldCount = await api.countEntries();

      const visibleTextsBefore = await entriesList.getVisibleEntryTexts();
      expect(visibleTextsBefore.length).toBeGreaterThan(0);

      // Create multiple entries in batch 0 to ensure count changes
      const {headword} = await api.createEntryAtIndex(midBatchIndex);
      for (let i = 0; i < entriesToAdd - 1; i++) {
        await api.createEntryAtIndex(midBatchIndex);
      }

      await entriesList.page.waitForTimeout(500);
      await entriesList.waitForSkeletonsToResolve();

      // Verify entry count increased
      const newCount = await api.countEntries();
      expect(newCount).toBe(oldCount + entriesToAdd);

      // View still shows valid entries (not skeletons or empty)
      const visibleTextsAfter = await entriesList.getVisibleEntryTexts();
      expect(visibleTextsAfter.length).toBeGreaterThan(0);
      for (const text of visibleTextsAfter) {
        expect(text.trim().length).toBeGreaterThan(0);
      }

      // New entry is at mid-batch index
      await entriesList.scrollToIndex(midBatchIndex);
      await expect(entriesList.entryWithText(headword)).toBeVisible({timeout: 5000});
    });

    test('updating uncached entry leaves visible entries unchanged', async () => {
      const indexBeyondLoadedBatches = BATCH_SIZE * 2;

      await entriesList.page.waitForTimeout(500);
      await entriesList.waitForSkeletonsToResolve();

      const initialVisibleTexts = await entriesList.getVisibleEntryTexts();
      expect(initialVisibleTexts.filter(t => t.length > 0).length).toBeGreaterThan(0);

      // Update entry beyond loaded batches
      const {id: entryId, updatedHeadword} = await api.updateEntryHeadword(indexBeyondLoadedBatches, '-UPDATED');
      await entriesList.page.waitForTimeout(300);

      // Batch 0 unchanged
      const newVisibleTexts = await entriesList.getVisibleEntryTexts();
      expect(newVisibleTexts).toEqual(initialVisibleTexts);

      // Find and verify updated entry
      const newIndex = await api.getEntryIndex(entryId);
      expect(newIndex).toBeGreaterThanOrEqual(0);

      await entriesList.scrollToIndex(newIndex);
      await expect(entriesList.entryWithText('-UPDATED')).toBeVisible({timeout: 5000});
      await expect(entriesList.entryWithText(updatedHeadword)).toBeVisible({timeout: 5000});
    });
  });
});
