import {type Page, expect, test} from '@playwright/test';

/**
 * This file is vibe coded.
 * Tests for EntriesList lazy loading and virtual scrolling.
 */

test.describe('EntriesList lazy loading', () => {
  function getLocators(page: Page) {
    return {
      vlist: page.locator('[role="table"] > div > div'),
      entryRows: page.locator('[role="table"] [role="row"]'),
      skeletons: page.locator('[role="table"] [data-skeleton]'),
    };
  }

  async function getVisibleEntryTexts(page: Page) {
    const {entryRows} = getLocators(page);
    const texts: string[] = [];
    const count = await entryRows.count();
    for (let i = 0; i < Math.min(count, 5); i++) {
      const text = await entryRows.nth(i).textContent();
      if (text) texts.push(text.trim());
    }
    return texts;
  }

  test.beforeEach(async ({page}) => {
    await page.goto('/testing/project-view');
    await waitForProjectViewReady(page);
  });

  test('entries are loaded initially', async ({page}) => {
    const {entryRows} = getLocators(page);

    await expect(entryRows.first()).toBeVisible();

    const visibleCount = await entryRows.count();
    expect(visibleCount).toBeGreaterThan(5);

    const firstEntry = entryRows.first();
    await expect(firstEntry).not.toHaveAttribute('data-skeleton');
    await expect(firstEntry).toContainText(/.+/);
  });

  test('can scroll through entries incrementally', async ({page}) => {
    const {vlist, skeletons} = getLocators(page);

    const initialTexts = await getVisibleEntryTexts(page);
    expect(initialTexts.length).toBeGreaterThan(0);

    // Scroll down 1000px
    await vlist.evaluate((el) => {
      el.scrollTop = 1000;
    });

    await page.waitForTimeout(500);

    const scrollTop = await vlist.evaluate((el) => el.scrollTop);
    expect(scrollTop).toBeGreaterThan(850);

    const newTexts = await getVisibleEntryTexts(page);
    expect(newTexts).not.toEqual(initialTexts);

    await expect(skeletons).toHaveCount(0);
  });

  test('large scroll jump loads new entries and preserves unloaded entries', async ({page}) => {
    const {vlist, entryRows, skeletons} = getLocators(page);

    await expect(entryRows.first()).toBeVisible();

    const scrollHeight = await vlist.evaluate((el) => el.scrollHeight);
    expect(scrollHeight).toBeGreaterThan(500);

    // Jump to 90%
    const targetScroll = scrollHeight * 0.9;
    await vlist.evaluate((el, target) => {
      el.scrollTop = target;
    }, targetScroll);

    await page.waitForTimeout(500);

    const scrollTop = await vlist.evaluate((el) => el.scrollTop);
    // Allow small margin for virtualization/browser rounding
    expect(scrollTop).toBeGreaterThan(targetScroll - 200);

    // Should resolve from skeletons to entries
    await expect(async () => {
      await expect(entryRows.first()).toBeVisible();
      const skeletonCount = await skeletons.count();
      expect(skeletonCount).toBeLessThan(3); // Allow a couple at edges
    }).toPass({timeout: 5000});

    // Now scroll back to middle
    const middleScroll = scrollHeight * 0.5;
    await vlist.evaluate((el, target) => {
      el.scrollTop = target;
    }, middleScroll);

    // Verify middle was not yet loaded and shows skeletons
    await page.waitForTimeout(100);
    await expect(skeletons.first()).toBeVisible();

    // Eventually loads content
    await expect(async () => {
      await expect(entryRows.first()).toBeVisible();
      await expect(skeletons).toHaveCount(0);
    }).toPass({timeout: 5000});

    await expect(entryRows.first()).not.toHaveAttribute('data-skeleton');
  });

  test('scroll to specific entry maintains entry visibility', async ({page}) => {
    const {vlist, entryRows} = getLocators(page);

    const firstEntry = entryRows.first();
    await firstEntry.click();
    const selectedText = await firstEntry.textContent();

    await vlist.evaluate((el) => {
      el.scrollTop = 2000;
    });

    await page.waitForTimeout(300);

    const scrollTop = await vlist.evaluate((el) => el.scrollTop);
    expect(scrollTop).toBeGreaterThan(1850);

    // Scroll back to top
    await vlist.evaluate((el) => {
      el.scrollTop = 0;
    });

    await page.waitForTimeout(500);

    const firstEntryAfter = entryRows.first();
    await expect(firstEntryAfter).toContainText(selectedText?.slice(0, 10) || '');
  });
});

async function waitForProjectViewReady(page: Page) {
  await expect(page.locator('.i-mdi-loading')).toHaveCount(0);
  await page.waitForFunction(() => document.fonts.ready);
  await expect(page.locator('[data-skeleton]')).toHaveCount(0, {timeout: 10000});
}
