import {type Page, expect, test} from '@playwright/test';
import {assertScreenshot} from './snapshot';

function filterLocator(page: Page) {
  return page.getByRole('textbox', {name: 'Filter'});
}

for (const colorScheme of ['light', 'dark'] as const) {
  test(`ui snapshot selected entry (color: ${colorScheme})`, async ({page}) => {
    await page.emulateMedia({colorScheme});
    await page.goto('/testing/project-view');
    await waitForProjectViewReady(page);
    await expect(filterLocator(page)).toBeVisible();
    await scrollToEntry(page, 'ambuka');
    await expect(page.getByRole('heading', {name: 'ambuka'})).toBeVisible();
    await expect(page.locator('.i-mdi-dots-vertical')).toBeVisible();
    await expect(page.locator('.i-mdi-loading')).toHaveCount(0);
    await assertScreenshot(page, 'project-view-entry-selected-' + colorScheme);
  });

  test(`ui snapshot default (color: ${colorScheme})`, async ({page}) => {
    await page.emulateMedia({colorScheme});
    await page.goto('/testing/project-view');
    await waitForProjectViewReady(page);
    await expect(filterLocator(page)).toBeVisible();
    await scrollToEntry(page, 'ambuka');
    await assertScreenshot(page, 'project-view-default-' + colorScheme);
  });
}

async function scrollToEntry(page: Page, search: string) {
  await filterLocator(page).fill(search);
  await page.getByRole('row', { name: search }).click();
  await filterLocator(page).clear();
}

async function waitForProjectViewReady(page: Page) {
  await expect(page.locator('.i-mdi-loading')).toHaveCount(0);
  await page.waitForFunction(() => document.fonts.ready);
  await expect(page.locator('.animate-pulse')).toHaveCount(0);
}
