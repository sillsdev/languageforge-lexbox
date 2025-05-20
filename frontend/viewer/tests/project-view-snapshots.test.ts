import {type Page, expect, test} from '@playwright/test';

const viewports = [
  {height: 720, width: 1280, name: 'medium'},
  {height: 812, width: 375, name: 'iphone-11'},
];

for (const viewport of viewports) {
  for (const colorScheme of ['light', 'dark'] as const) {
    test(`ui snapshot selected entry (viewport: ${viewport.name} - ${colorScheme})`, async ({page}) => {
      await page.emulateMedia({colorScheme});
      await page.setViewportSize(viewport);
      await page.goto('/testing/project-view');
      await waitForProjectViewReady(page);
      await page.getByRole('row', {name: 'ambuka to cross a body of'}).click();
      await expect(page.getByRole('heading', {name: 'ambuka'})).toBeVisible();
      await expect(page.locator('.i-mdi-dots-vertical')).toBeVisible();
      await expect(page.locator('.i-mdi-loading')).not.toBeVisible();
      await expect(page).toHaveScreenshot();
    });
    test(`ui snapshot default (viewport: ${viewport.name} - ${colorScheme})`, async ({page}) => {
      await page.emulateMedia({colorScheme});
      await page.setViewportSize(viewport);
      await page.goto('/testing/project-view');
      await waitForProjectViewReady(page);
      await expect(page.locator('.animate-pulse')).not.toBeVisible();
      await expect(page.locator('.i-mdi-loading')).not.toBeVisible();
      await expect(page.getByRole('textbox', {name: 'Filter'})).toBeVisible();
      await expect(page).toHaveScreenshot({});
    });
  }
}

async function waitForProjectViewReady(page: Page) {
  await expect(page.locator('.i-mdi-loading')).not.toBeVisible();
  await page.waitForFunction(() => document.fonts.ready);
  await expect(page.locator('.animate-pulse')).not.toBeVisible();
}
