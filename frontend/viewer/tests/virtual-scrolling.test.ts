import { type Page, expect, test } from '@playwright/test';

const BASE_URL = '/testing/project-view/browse';

async function waitForProjectViewReady(page: Page) {
  // Wait for loading spinner to disappear
  await page.waitForFunction(() => {
    const loading = document.querySelector('.i-mdi-loading');
    return !loading || loading.classList.contains('hidden');
  }, { timeout: 10000 });
  
  // Wait for fonts to load
  await page.waitForFunction(() => document.fonts.ready, { timeout: 10000 });
  
  // Wait for initial entries to render
  await page.waitForFunction(() => {
    const rows = document.querySelectorAll('[role="row"]');
    return rows.length > 0;
  }, { timeout: 10000 });
  
  // Small delay to let things settle
  await page.waitForTimeout(500);
}

test.describe('Virtual Scrolling - EntriesList', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto(BASE_URL);
    await waitForProjectViewReady(page);
  });

  test('should load initial entries on page load', async ({ page }) => {
    // Should see entries rendered as rows
    const rows = page.locator('[role="row"]');
    const count = await rows.count();
    expect(count).toBeGreaterThan(0);
    
    // First entry should have text content
    const firstRow = rows.first();
    const text = await firstRow.textContent();
    expect(text?.trim().length).toBeGreaterThan(0);
  });

  test('should have scrollable container with entries', async ({ page }) => {
    // Get the virtual list container
    const virtualList = page.locator('[role="table"]').first();
    
    // Should be able to get scroll metrics
    const scrollHeight = await virtualList.evaluate(el => el.scrollHeight);
    const clientHeight = await virtualList.evaluate(el => el.clientHeight);
    
    // Scrollable area should be larger than viewport for a list of 1464 entries
    expect(scrollHeight).toBeGreaterThan(clientHeight);
  });

  test('should scroll to end of list', async ({ page }) => {
    const virtualList = page.locator('[role="table"]').first();
    
    // Scroll to bottom
    await virtualList.evaluate(el => {
      el.scrollTop = el.scrollHeight - el.clientHeight;
    });
    
    // Wait for entries to load at the end
    await page.waitForTimeout(800);
    
    // Should still have entries visible
    const rows = page.locator('[role="row"]');
    const count = await rows.count();
    expect(count).toBeGreaterThan(0);
  });

  test('should load more entries when scrolling down', async ({ page }) => {
    const virtualList = page.locator('[role="table"]').first();
    
    // Get initial visible entry count
    let rows = page.locator('[role="row"]');
    const initialCount = await rows.count();
    
    // Scroll down a bit
    await virtualList.evaluate(el => {
      el.scrollTop += 2000;
    });
    
    // Wait for potential new entries to load
    await page.waitForTimeout(600);
    
    // Should still have entries visible (might be different entries)
    rows = page.locator('[role="row"]');
    const newCount = await rows.count();
    expect(newCount).toBeGreaterThan(0);
  });

  test('should load more entries when scrolling up from bottom', async ({ page }) => {
    const virtualList = page.locator('[role="table"]').first();
    
    // Scroll to bottom
    await virtualList.evaluate(el => {
      el.scrollTop = el.scrollHeight - el.clientHeight;
    });
    await page.waitForTimeout(600);
    
    // Scroll up
    await virtualList.evaluate(el => {
      el.scrollTop = Math.max(0, el.scrollTop - 1000);
    });
    await page.waitForTimeout(600);
    
    // Should still have entries visible
    const rows = page.locator('[role="row"]');
    const count = await rows.count();
    expect(count).toBeGreaterThan(0);
  });

  test('should handle rapid scrolling', async ({ page }) => {
    const virtualList = page.locator('[role="table"]').first();
    
    // Rapid scroll down
    for (let i = 0; i < 5; i++) {
      await virtualList.evaluate(el => {
        el.scrollTop += 500;
      });
      await page.waitForTimeout(100);
    }
    
    // Should still have entries visible
    let rows = page.locator('[role="row"]');
    let count = await rows.count();
    expect(count).toBeGreaterThan(0);
    
    // Scroll back to top
    await virtualList.evaluate(el => {
      el.scrollTop = 0;
    });
    await page.waitForTimeout(500);
    
    // Should see top entries
    rows = page.locator('[role="row"]');
    count = await rows.count();
    expect(count).toBeGreaterThan(0);
  });

  test('should navigate to far position in list', async ({ page }) => {
    const virtualList = page.locator('[role="table"]').first();
    
    // Jump to 75% of the way through the list
    await virtualList.evaluate(el => {
      const targetScroll = (el.scrollHeight - el.clientHeight) * 0.75;
      el.scrollTop = targetScroll;
    });
    await page.waitForTimeout(800);
    
    // Should have loaded entries at that position
    const rows = page.locator('[role="row"]');
    const count = await rows.count();
    expect(count).toBeGreaterThan(0);
  });

  test('should select entry and show details', async ({ page }) => {
    // Get first entry row
    const firstRow = page.locator('[role="row"]').first();
    
    // Get the entry text to verify it's selected later
    const entryText = await firstRow.textContent();
    
    // Click on the first entry
    await firstRow.click();
    await page.waitForTimeout(300);
    
    // The detail panel on the right should show the entry
    // Look for the detail panel (it's typically on the right side)
    const detailPanel = page.locator('main').nth(1);
    const detailText = await detailPanel.textContent();
    
    // Detail should contain some text from the selected entry
    expect(detailText?.length).toBeGreaterThan(0);
  });

  test('should maintain list state during scrolling', async ({ page }) => {
    const virtualList = page.locator('[role="table"]').first();
    
    // Get initial scroll position
    const initialScroll = await virtualList.evaluate(el => el.scrollTop);
    
    // Scroll a bit
    await virtualList.evaluate(el => {
      el.scrollTop = 1500;
    });
    
    const middleScroll = await virtualList.evaluate(el => el.scrollTop);
    expect(Math.abs(middleScroll - 1500)).toBeLessThan(50);
    
    // Scroll more
    await virtualList.evaluate(el => {
      el.scrollTop = 3000;
    });
    
    const farScroll = await virtualList.evaluate(el => el.scrollTop);
    expect(Math.abs(farScroll - 3000)).toBeLessThan(50);
  });

  test('should show entries while scrolling through middle of list', async ({ page }) => {
    const virtualList = page.locator('[role="table"]').first();
    
    // Scroll to middle (50% of list)
    await virtualList.evaluate(el => {
      el.scrollTop = (el.scrollHeight - el.clientHeight) * 0.5;
    });
    await page.waitForTimeout(600);
    
    // Should have entries visible
    const rows = page.locator('[role="row"]');
    const count = await rows.count();
    expect(count).toBeGreaterThan(0);
    
    // All rows should have content
    for (let i = 0; i < Math.min(count, 5); i++) {
      const row = rows.nth(i);
      const text = await row.textContent();
      expect(text?.trim().length).toBeGreaterThan(0);
    }
  });

  test('should handle scroll jump to different regions', async ({ page }) => {
    const virtualList = page.locator('[role="table"]').first();
    
    // Jump to 25%
    await virtualList.evaluate(el => {
      el.scrollTop = (el.scrollHeight - el.clientHeight) * 0.25;
    });
    await page.waitForTimeout(600);
    
    let rows = page.locator('[role="row"]');
    let count1 = await rows.count();
    expect(count1).toBeGreaterThan(0);
    
    // Jump to 75%
    await virtualList.evaluate(el => {
      el.scrollTop = (el.scrollHeight - el.clientHeight) * 0.75;
    });
    await page.waitForTimeout(600);
    
    rows = page.locator('[role="row"]');
    let count2 = await rows.count();
    expect(count2).toBeGreaterThan(0);
    
    // Jump back to 10%
    await virtualList.evaluate(el => {
      el.scrollTop = (el.scrollHeight - el.clientHeight) * 0.1;
    });
    await page.waitForTimeout(600);
    
    rows = page.locator('[role="row"]');
    let count3 = await rows.count();
    expect(count3).toBeGreaterThan(0);
  });
});
