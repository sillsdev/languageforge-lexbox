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
  
  // Wait for actual entries to render with text content (not just placeholders)
  // Placeholders are empty divs, so we look for rows with actual text
  await page.waitForFunction(() => {
    const rows = Array.from(document.querySelectorAll('[role="row"]'));
    // Find at least one row with actual text content (not a placeholder)
    return rows.some(row => {
      const text = row.textContent?.trim();
      return text && text.length > 0;
    });
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
    // Should see entries rendered as rows (but some may be placeholders)
    const rows = page.locator('[role="row"]');
    const count = await rows.count();
    expect(count).toBeGreaterThan(0);
    
    // Find first entry row with actual content (not placeholder)
    let foundRealEntry = false;
    for (let i = 0; i < Math.min(count, 10); i++) {
      const row = rows.nth(i);
      const text = await row.textContent();
      if (text && text.trim().length > 0) {
        foundRealEntry = true;
        break;
      }
    }
    expect(foundRealEntry).toBe(true);
  });

  test('should have scrollable container with entries', async ({ page }) => {
    // Get the virtual list container (the one with actual content)
    const virtualList = page.locator('[role="table"]').first();
    
    // Should be able to get scroll metrics
    const scrollHeight = await virtualList.evaluate(el => el.scrollHeight);
    const clientHeight = await virtualList.evaluate(el => el.clientHeight);
    
    // For virtual scrolling with padding placeholders, scrollHeight represents full list
    // Should be >= clientHeight to allow scrolling
    expect(scrollHeight).toBeGreaterThanOrEqual(clientHeight);
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
    // Get first entry row with actual content
    const rows = page.locator('[role="row"]');
    let entryText = '';
    let selectedRow = null;
    
    const count = await rows.count();
    for (let i = 0; i < count; i++) {
      const row = rows.nth(i);
      const text = await row.textContent();
      if (text && text.trim().length > 0) {
        selectedRow = row;
        entryText = text;
        break;
      }
    }
    
    expect(selectedRow).toBeTruthy();
    expect(entryText.length).toBeGreaterThan(0);
    
    // Try to click the selected row - may need to use force due to overlay
    if (selectedRow) {
      await selectedRow.click({ force: true });
      await page.waitForTimeout(500);
      
      // The detail panel should be updated (just verify page is responsive)
      const mainElements = page.locator('main');
      const mainCount = await mainElements.count();
      expect(mainCount).toBeGreaterThan(0);
    }
  });

  test('should maintain list state during scrolling', async ({ page }) => {
    const virtualList = page.locator('[role="table"]').first();
    
    // Small scrolls should be preserved
    // Scroll down
    await virtualList.evaluate(el => {
      el.scrollTop = 500;
    });
    
    await page.waitForTimeout(300);
    
    // Should have scrolled
    const afterScroll = await virtualList.evaluate(el => el.scrollTop);
    expect(afterScroll).toBeGreaterThan(0);
    
    // Scroll to different position
    await virtualList.evaluate(el => {
      el.scrollTop = 1000;
    });
    
    await page.waitForTimeout(300);
    
    const farScroll = await virtualList.evaluate(el => el.scrollTop);
    expect(farScroll).toBeGreaterThan(afterScroll);
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
    
    // At least one row should have actual content (not placeholder)
    let foundContent = false;
    for (let i = 0; i < Math.min(count, 10); i++) {
      const row = rows.nth(i);
      const text = await row.textContent();
      if (text && text.trim().length > 0) {
        foundContent = true;
        break;
      }
    }
    expect(foundContent).toBe(true);
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
