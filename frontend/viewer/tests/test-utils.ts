import {type Page, expect} from '@playwright/test';

export async function waitForProjectViewReady(page: Page, waitForTestUtils = false) {
  await expect(page.locator('.i-mdi-loading')).toHaveCount(0, {timeout: 10000});
  await page.waitForFunction(() => document.fonts.ready);
  await expect(page.locator('[data-skeleton]')).toHaveCount(0, {timeout: 10000});
  // Wait for test utilities to be available if requested
  if (waitForTestUtils) {
    await page.waitForFunction(() => window.__PLAYWRIGHT_UTILS__?.demoApi, {timeout: 5000});
  }
}
