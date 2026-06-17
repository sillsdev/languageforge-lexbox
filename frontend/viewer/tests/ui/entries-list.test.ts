import {expect, test} from '@playwright/test';

import {DemoProjectPage} from './demo-project.page';

const ESTIMATED_ITEM_HEIGHT = 60;
const BATCH_SIZE = 50;

test.describe('EntriesList', () => {
  let projectPage: DemoProjectPage;

  test.describe('Lazy loading', () => {
    test.beforeEach(async ({page}) => {
      projectPage = new DemoProjectPage(page);
      await projectPage.goto();
    });

    test('entries are loaded initially', async () => {
      await expect(projectPage.entriesList.entryRows.first()).toBeVisible();

      const visibleCount = await projectPage.entriesList.entryRows.count();
      expect(visibleCount).toBeGreaterThan(5);

      await expect(projectPage.entriesList.entryRows.first()).not.toHaveAttribute('data-skeleton');
      await expect(projectPage.entriesList.entryRows.first()).toContainText(/.+/);
    });

    test('can scroll through entries incrementally', async () => {
      const initialTexts = await projectPage.entriesList.getVisibleEntryTexts(5);
      expect(initialTexts.length).toBeGreaterThan(0);

      await projectPage.entriesList.scrollToPixels(1000);

      await expect(async () => {
        const scrollTop = await projectPage.entriesList.getScrollTop();
        expect(scrollTop).toBeGreaterThan(850);
      }).toPass({timeout: 2000});

      const newTexts = await projectPage.entriesList.getVisibleEntryTexts(5);
      expect(newTexts).not.toEqual(initialTexts);

      await expect(projectPage.entriesList.skeletons).toHaveCount(0);
    });

    test('large scroll jump loads new entries and preserves unloaded entries', async () => {
      await expect(projectPage.entriesList.entryRows.first()).toBeVisible();

      const scrollHeight = await projectPage.entriesList.getScrollHeight();
      expect(scrollHeight).toBeGreaterThan(500);

      // Jump to 90% of list
      const targetScroll = scrollHeight * 0.9;
      await projectPage.entriesList.scrollToPixels(targetScroll);

      await expect(async () => {
        const scrollTop = await projectPage.entriesList.getScrollTop();
        expect(scrollTop).toBeGreaterThan(targetScroll - 200);
      }).toPass({timeout: 2000});

      // Should resolve from skeletons to entries
      await expect(async () => {
        await expect(projectPage.entriesList.entryRows.first()).toBeVisible();
        const skeletonCount = await projectPage.entriesList.skeletons.count();
        expect(skeletonCount).toBeLessThan(3);
      }).toPass({timeout: 5000});

      // Scroll back to middle (should show skeletons then load)
      await projectPage.entriesList.scrollToPercent(0.5);

      // Middle was not loaded yet - verify skeletons appear briefly
      await projectPage.page.waitForTimeout(100);
      await expect(projectPage.entriesList.skeletons.first()).toBeVisible();

      // Eventually loads content
      await expect(async () => {
        await expect(projectPage.entriesList.entryRows.first()).toBeVisible();
        await expect(projectPage.entriesList.skeletons).toHaveCount(0);
      }).toPass({timeout: 5000});

      await expect(projectPage.entriesList.entryRows.first()).not.toHaveAttribute('data-skeleton');
    });
  });

  test.describe('Jump to entry', () => {
    test.beforeEach(async ({page}) => {
      projectPage = new DemoProjectPage(page);
      await projectPage.goto();
    });

    test('reloading with entry selected scrolls to that entry', async ({page}) => {
      // Scroll far down (~100 items)
      await projectPage.entriesList.scrollToPixels(100 * ESTIMATED_ITEM_HEIGHT);
      await projectPage.page.waitForTimeout(200);
      await projectPage.entriesList.waitForSkeletonsToResolve();

      // Select an entry that's visible (get text before click to avoid DOM recycling issues)
      const selectedText = await projectPage.entriesList.selectEntryByIndex(6);
      expect(selectedText).toBeTruthy();

      expect(page.url()).toContain('entryId=');

      // Reload and verify selected entry is still visible
      await page.reload();
      await projectPage.entriesList.waitForSkeletonsToResolve();

      await expect(projectPage.entriesList.selectedEntry).toBeVisible({timeout: 5000});
      const expectedSnippet = selectedText.slice(0, 20);
      await expect(projectPage.entriesList.selectedEntry).toContainText(expectedSnippet);
    });

    test('clearing search filter with entry selected keeps entry visible', async () => {
      // Scroll far into the list (~500 items down)
      await projectPage.entriesList.scrollToPixels(500 * ESTIMATED_ITEM_HEIGHT);
      await projectPage.page.waitForTimeout(200);
      await projectPage.entriesList.waitForSkeletonsToResolve();

      // Get headword from visible entry
      const entryText = await projectPage.entriesList.entryRows.first().textContent();
      const headword = entryText?.split(/\s+/).filter(Boolean)[0] ?? '';
      expect(headword.length).toBeGreaterThan(0);

      // Filter and select
      await projectPage.entriesList.filterByText(headword);
      await projectPage.entriesList.entryRows.first().click();
      await expect(projectPage.entriesList.entryRows.first()).toContainText(headword);

      // Clear filter - selected entry should still be visible
      await projectPage.entriesList.clearFilter();

      await expect(projectPage.entriesList.selectedEntry).toBeVisible({timeout: 10000});
      await expect(projectPage.entriesList.selectedEntry).toContainText(headword);
    });

    test('searching an existing entry after load shows it', async () => {
      const {headword} = await projectPage.api.getEntryWithEnglishGloss();
      expect(headword.length).toBeGreaterThan(0);

      await projectPage.entriesList.searchInput.fill(headword);
      await expect(projectPage.entriesList.skeletons).toHaveCount(0);

      const entryRow = projectPage.entriesList.entryRows
        .filter({hasText: headword})
        .filter({hasNotText: 'Add to dictionary'});
      const createFromSearchRow = projectPage.entriesList.entryRows
        .filter({hasText: 'Add to dictionary'});

      await expect(projectPage.entriesList.entryRows).toHaveCount(2);
      await expect(entryRow).toBeVisible();
      await expect(createFromSearchRow).toBeVisible();
    });
  });

  test.describe('Entry event handling', () => {
    test.beforeEach(async ({page}) => {
      projectPage = new DemoProjectPage(page);
      await projectPage.goto();
    });

    test('entry delete event removes entry from list without full reset', async () => {
      const initialTexts = await projectPage.entriesList.getVisibleEntryTexts();
      expect(initialTexts.length).toBeGreaterThan(2);

      const firstEntryId = await projectPage.api.getEntryIdAtIndex(0);
      await projectPage.api.deleteEntry(firstEntryId);

      await expect(async () => {
        const newTexts = await projectPage.entriesList.getVisibleEntryTexts();
        expect(newTexts[0]).toBe(initialTexts[1]);
        expect(newTexts).not.toContain(initialTexts[0]);
      }).toPass({timeout: 5000});
    });

    test('entry update event updates entry in list without full reset', async () => {
      const firstEntryText = await projectPage.entriesList.entryRows.first().textContent();
      expect(firstEntryText).toBeTruthy();

      // Update first entry by prepending to its headword (so it stays at index 0)
      const {updatedHeadword} = await projectPage.api.updateEntryHeadwordPrepend(0, '---UPDATED---');

      // The first entry in UI should now show the updated text
      await expect(async () => {
        const newFirstEntryText = await projectPage.entriesList.entryRows.first().textContent();
        expect(newFirstEntryText).toContain('---UPDATED---');
      }).toPass({timeout: 5000});

      await expect(projectPage.entriesList.entryWithText(updatedHeadword)).toBeVisible();
    });

    test('deleting selected entry clears selection', async () => {
      await projectPage.entriesList.entryRows.first().click();
      await expect(projectPage.entriesList.selectedEntry).toBeVisible();

      const entryId = await projectPage.api.getEntryIdAtIndex(0);
      await projectPage.api.deleteEntry(entryId);

      await projectPage.page.waitForTimeout(300);
      await expect(projectPage.entriesList.selectedEntry).not.toBeAttached();
    });

    test('deleting entry not in cache triggers reset but maintains position', async () => {
      // Scroll to middle
      await projectPage.entriesList.scrollToPercent(0.5);
      await projectPage.entriesList.waitForSkeletonsToResolve();

      const visibleTexts = await projectPage.entriesList.getVisibleEntryTexts();
      expect(visibleTexts.length).toBeGreaterThan(0);

      // Delete entry from top (not in cache)
      const topEntryId = await projectPage.api.getEntryIdAtIndex(0);
      await projectPage.api.deleteEntry(topEntryId);

      await projectPage.page.waitForTimeout(500);
      await projectPage.entriesList.waitForSkeletonsToResolve();

      // Should still have visible entries after reset
      const newVisibleTexts = await projectPage.entriesList.getVisibleEntryTexts();
      expect(newVisibleTexts.length).toBeGreaterThan(0);
    });

    test('adding entry in loaded batch updates UI in place', async () => {
      const midBatchIndex = Math.floor(BATCH_SIZE / 2);
      const lastIndexInBatch0 = BATCH_SIZE - 1;

      const oldMidText = await projectPage.api.getHeadwordAtIndex(midBatchIndex);
      const oldLastText = await projectPage.api.getHeadwordAtIndex(lastIndexInBatch0);

      const {headword} = await projectPage.api.createEntryAtIndex(midBatchIndex);
      await projectPage.page.waitForTimeout(300);

      // Verify API reflects the insertion
      const newMidText = await projectPage.api.getHeadwordAtIndex(midBatchIndex);
      expect(newMidText).toBe(headword);

      const shiftedMidText = await projectPage.api.getHeadwordAtIndex(midBatchIndex + 1);
      expect(shiftedMidText).toBe(oldMidText);

      const shiftedLastText = await projectPage.api.getHeadwordAtIndex(lastIndexInBatch0 + 1);
      expect(shiftedLastText).toBe(oldLastText);

      // Verify UI shows the new entry
      await projectPage.scrollToIndex(midBatchIndex);
      await expect(projectPage.entriesList.entryWithText(headword)).toBeVisible({timeout: 5000});

      await projectPage.scrollToIndex(lastIndexInBatch0 + 1);
      await expect(projectPage.entriesList.entryWithText(oldLastText)).toBeVisible({timeout: 5000});
    });

    test('adding entry at end increases scroll height without affecting visible entries', async () => {
      const initialVisibleTexts = await projectPage.entriesList.getVisibleEntryTexts();
      const oldScrollHeight = await projectPage.entriesList.getScrollHeight();

      const {headword: lastHeadword, index: lastIndex} = await projectPage.api.getLastEntry();
      const newHeadword = lastHeadword + 'z-inserted';
      await projectPage.api.createEntryWithHeadword(newHeadword);

      // Wait for scroll height to increase
      await expect(async () => {
        const newHeight = await projectPage.entriesList.getScrollHeight();
        expect(newHeight).toBeGreaterThan(oldScrollHeight);
      }).toPass({timeout: 5000});

      // Visible entries unchanged (batch 0 not affected)
      const newVisibleTexts = await projectPage.entriesList.getVisibleEntryTexts();
      expect(newVisibleTexts).toEqual(initialVisibleTexts);

      // Verify via API
      const entryAtEnd = await projectPage.api.getHeadwordAtIndex(lastIndex + 1);
      expect(entryAtEnd).toBe(newHeadword);

      // Scroll to end and verify visible
      await projectPage.entriesList.scrollToEnd();
      await expect(projectPage.entriesList.entryWithText(newHeadword)).toBeVisible({timeout: 10000});
    });

    test('entry added before loaded batch triggers quiet reset', async () => {
      const midBatchIndex = Math.floor(BATCH_SIZE / 2);
      const indexInBatch2 = BATCH_SIZE * 2 + 20;
      const entriesToAdd = 10;

      // Scroll to batch 2
      await projectPage.scrollToIndex(indexInBatch2);
      const oldCount = await projectPage.api.countEntries();

      const visibleTextsBefore = await projectPage.entriesList.getVisibleEntryTexts();
      expect(visibleTextsBefore.length).toBeGreaterThan(0);

      // Create multiple entries in batch 0 to ensure count changes
      const {headword} = await projectPage.api.createEntryAtIndex(midBatchIndex);
      for (let i = 0; i < entriesToAdd - 1; i++) {
        await projectPage.api.createEntryAtIndex(midBatchIndex);
      }

      await projectPage.page.waitForTimeout(500);
      await projectPage.entriesList.waitForSkeletonsToResolve();

      // Verify entry count increased
      const newCount = await projectPage.api.countEntries();
      expect(newCount).toBe(oldCount + entriesToAdd);

      // View still shows valid entries (not skeletons or empty)
      const visibleTextsAfter = await projectPage.entriesList.getVisibleEntryTexts();
      expect(visibleTextsAfter.length).toBeGreaterThan(0);
      for (const text of visibleTextsAfter) {
        expect(text.trim().length).toBeGreaterThan(0);
      }

      // New entry is at mid-batch index
      await projectPage.scrollToIndex(midBatchIndex);
      await expect(projectPage.entriesList.entryWithText(headword)).toBeVisible({timeout: 5000});
    });

    test('updating uncached entry leaves visible entries unchanged', async () => {
      const indexBeyondLoadedBatches = BATCH_SIZE * 2;

      await projectPage.page.waitForTimeout(500);
      await projectPage.entriesList.waitForSkeletonsToResolve();

      const initialVisibleTexts = await projectPage.entriesList.getVisibleEntryTexts();
      expect(initialVisibleTexts.filter(t => t.length > 0).length).toBeGreaterThan(0);

      // Update entry beyond loaded batches
      const {id: entryId, updatedHeadword} = await projectPage.api.updateEntryHeadword(indexBeyondLoadedBatches, '-UPDATED');
      await projectPage.page.waitForTimeout(300);

      // Batch 0 unchanged
      const newVisibleTexts = await projectPage.entriesList.getVisibleEntryTexts();
      expect(newVisibleTexts).toEqual(initialVisibleTexts);

      // Find and verify updated entry
      const newIndex = await projectPage.api.getEntryIndex(entryId);
      expect(newIndex).toBeGreaterThanOrEqual(0);

      await projectPage.scrollToIndex(newIndex);
      await expect(projectPage.entriesList.entryWithText('-UPDATED')).toBeVisible({timeout: 5000});
      await expect(projectPage.entriesList.entryWithText(updatedHeadword)).toBeVisible({timeout: 5000});
    });
  });
});
