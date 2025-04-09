import {test, expect} from '@playwright/test';


const viewports = [
  {height: 720, width: 1280, name: 'medium'},
  {height: 812, width: 375, name: 'iphone-11'},
];
for (let viewport of viewports) {
  test(`ui snapshot selected entry, view port:${viewport.name}`, async ({page}) => {
    await page.setViewportSize(viewport);
    await page.goto('/testing/project-view');
    await page.getByRole('row', {name: 'ambuka to cross a body of'}).click();
    await expect(page.getByRole('heading', {name: 'ambuka'})).toBeVisible();
    await expect(page.locator('.i-mdi-dots-vertical')).toBeVisible();
    await expect(page.locator('.i-mdi-loading')).not.toBeVisible();
    await expect(page).toHaveScreenshot();
  });
  test(`ui snapshot default, view port:${viewport.name}`, async ({page}) => {
    await page.setViewportSize(viewport);
    await page.goto('/testing/project-view');
    await expect(page.locator('.i-mdi-loading')).not.toBeVisible();
    await expect(page.getByRole('textbox', {name: 'Filter'})).toBeVisible();
    await expect(page).toHaveScreenshot();
  });
}

