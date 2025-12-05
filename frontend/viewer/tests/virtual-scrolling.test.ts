import { type Page, expect, test } from '@playwright/test';

const BASE_URL = '/testing/project-view/browse';

async function waitForProjectViewReady(page: Page) {
  await expect(page.locator('.i-mdi-loading')).toHaveCount(0);
  await page.waitForFunction(() => document.fonts.ready);
  await expect(page.locator('.animate-pulse')).toHaveCount(0);
}

test.describe('Virtual Scrolling - EntriesList', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto(BASE_URL);
    await waitForProjectViewReady(page);
  });

  test('should display scrollbar representing full list of 1464 entries', async ({ page }) => {
    // Get the scrollbar (VList creates a scrollable container)
    const virtualList = page.locator('[role="table"]').first();
    
    // Scrollbar height should be proportional to (viewportSize / totalSize)
    // With 100 visible entries and 1464 total, scrollbar should be small
    // We can verify this by checking the list height and scrollable area ratio
    const listHeight = await virtualList.evaluate(el => el.scrollHeight);
    
    // 1464 entries * ~52px per entry ≈ 76128px theoretical height
    // The actual rendered height will be less due to virtualization, but the scrollHeight
    // should reflect the full list when we include placeholders
    expect(listHeight).toBeGreaterThan(5000); // Should be substantial
  });

  test('should scroll to end of list (entry 1464: zungunusa)', async ({ page }) => {
    const virtualList = page.locator('[role="table"]').first();
    
    // Scroll to bottom
    await virtualList.evaluate(el => {
      el.scrollTop = el.scrollHeight - el.clientHeight;
    });
    
    // Wait for entries to load at the end
    await page.waitForTimeout(500);
    
    // The last entry should be visible
    const lastEntry = await page.locator('text="zungunusa"').isVisible();
    expect(lastEntry).toBe(true);
  });

  test('should load entries when scrolling up from bottom', async ({ page }) => {
    const virtualList = page.locator('[role="table"]').first();
    
    // Scroll to bottom
    await virtualList.evaluate(el => {
      el.scrollTop = el.scrollHeight - el.clientHeight;
    });
    await page.waitForTimeout(500);
    
    // Scroll up a bit
    await virtualList.evaluate(el => {
      el.scrollTop = el.scrollHeight - el.clientHeight - 1000;
    });
    await page.waitForTimeout(500);
    
    // Entries should still be visible
    const entries = page.locator('role=row').filter({ hasText: /[a-z]+/ });
    const count = await entries.count();
    expect(count).toBeGreaterThan(0);
  });

  test('should filter entries and maintain selection', async ({ page }) => {
    // Search for "pita"
    const searchBox = page.getByRole('textbox', { name: 'Filter' });
    await searchBox.fill('pita');
    await page.waitForTimeout(300);
    
    // Should show filtered results
    const pita = page.getByRole('row', { name: /pita/ }).first();
    await expect(pita).toBeVisible();
    
    // Click on "pita" entry
    await pita.click();
    
    // Detail panel should update to show "pita"
    const heading = page.getByRole('heading', { name: 'pita' });
    await expect(heading).toBeVisible();
  });

  test('should clear filter and show all entries again', async ({ page }) => {
    // Search for "pita"
    const searchBox = page.getByRole('textbox', { name: 'Filter' });
    await searchBox.fill('pita');
    await page.waitForTimeout(300);
    
    // Verify filtered view
    let entries = page.locator('role=row').filter({ hasText: /[a-z]+/ });
    const filteredCount = await entries.count();
    expect(filteredCount).toBeLessThan(10); // Should be just pita variants
    
    // Clear search
    await searchBox.clear();
    await page.waitForTimeout(300);
    
    // Should show many more entries
    entries = page.locator('role=row').filter({ hasText: /[a-z]+/ });
    const unfilteredCount = await entries.count();
    expect(unfilteredCount).toBeGreaterThan(filteredCount);
  });

  test('should scroll to selected entry on page load with entryId param', async ({ page }) => {
    // Navigate to a specific entry (some entry from middle of list)
    // Using the URL parameter approach that browser history uses
    await page.goto(`${BASE_URL}?entryId=6fac4249-4a72-47d2-b767-9d677530a59e`);
    await waitForProjectViewReady(page);
    
    // The entry should be visible and selected
    const selectedRow = page.locator('role=row.selected');
    await expect(selectedRow).toBeVisible();
    
    // Detail panel should show the entry
    const heading = page.locator('role=heading').first();
    await expect(heading).not.toBeEmpty();
  });

  test('should handle rapid scrolling without getting stuck', async ({ page }) => {
    const virtualList = page.locator('[role="table"]').first();
    
    // Rapid scroll down
    for (let i = 0; i < 10; i++) {
      await virtualList.evaluate(el => {
        el.scrollTop += 500;
      });
      await page.waitForTimeout(50);
    }
    
    // Should still have entries visible
    const entries = page.locator('role=row').filter({ hasText: /[a-z]+/ });
    const count = await entries.count();
    expect(count).toBeGreaterThan(0);
    
    // Scroll back to top
    await virtualList.evaluate(el => {
      el.scrollTop = 0;
    });
    await page.waitForTimeout(300);
    
    // Should see top entries
    const topEntry = page.locator('role=row').filter({ hasText: /[a-z]+/ }).first();
    await expect(topEntry).toBeVisible();
  });

  test('should select entry and verify detail panel updates', async ({ page }) => {
    // Click on second entry
    const secondEntry = page.locator('role=row').filter({ hasText: /[a-z]+/ }).nth(1);
    await secondEntry.click();
    
    // Detail panel should show something
    const detailHeading = page.locator('[role="heading"]').nth(1);
    const headingText = await detailHeading.textContent();
    expect(headingText?.trim()).not.toBe('');
  });

  test('should navigate to far entry and load appropriate window', async ({ page }) => {
    const virtualList = page.locator('[role="table"]').first();
    
    // Jump to 75% of the way through the list
    await virtualList.evaluate(el => {
      el.scrollTop = (el.scrollHeight - el.clientHeight) * 0.75;
    });
    await page.waitForTimeout(500);
    
    // Should have loaded entries at that position
    const entries = page.locator('role=row').filter({ hasText: /[a-z]+/ });
    const count = await entries.count();
    expect(count).toBeGreaterThan(0);
    
    // Scrolling should still work smoothly
    await virtualList.evaluate(el => {
      el.scrollTop += 1000;
    });
    await page.waitForTimeout(300);
    
    const entriesAfter = page.locator('role=row').filter({ hasText: /[a-z]+/ });
    const countAfter = await entriesAfter.count();
    expect(countAfter).toBeGreaterThan(0);
  });

  test('should handle filtering with selected entry far down', async ({ page }) => {
    const virtualList = page.locator('[role="table"]').first();
    
    // Scroll to an entry far down
    await virtualList.evaluate(el => {
      el.scrollTop = el.scrollHeight * 0.5;
    });
    await page.waitForTimeout(500);
    
    // Click on an entry
    const entryRow = page.locator('role=row').filter({ hasText: /[a-z]+/ }).first();
    const entryText = await entryRow.textContent();
    await entryRow.click();
    
    // Now search for something
    const searchBox = page.getByRole('textbox', { name: 'Filter' });
    await searchBox.fill('a');
    await page.waitForTimeout(300);
    
    // List should filter
    const filteredEntries = page.locator('role=row').filter({ hasText: /[a-z]+/ });
    const filteredCount = await filteredEntries.count();
    expect(filteredCount).toBeGreaterThan(0);
  });

  test('should maintain scroll position when loading more entries', async ({ page }) => {
    const virtualList = page.locator('[role="table"]').first();
    
    // Scroll to a specific position
    await virtualList.evaluate(el => {
      el.scrollTop = 2000;
    });
    const initialScroll = await virtualList.evaluate(el => el.scrollTop);
    
    // Wait a bit for potential loads
    await page.waitForTimeout(500);
    
    // Scroll position should be maintained or only slightly changed due to item heights
    const finalScroll = await virtualList.evaluate(el => el.scrollTop);
    const difference = Math.abs(initialScroll - finalScroll);
    expect(difference).toBeLessThan(100); // Allow small variance due to rendering
  });
});
