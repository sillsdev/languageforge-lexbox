import {expect, test} from '@playwright/test';
import {blurFocusedField, docsScreenshot, openStory} from './docs-screenshot';

// docs/site/fw-lite/getting-started.md. Names are what the page references; renaming one breaks it.

test.describe('home screen', () => {
  // The home screen centers a fixed-width column; a narrow window keeps the shot from being mostly margin.
  test.use({viewport: {width: 720, height: 640}});

test('home screen, logged out', async ({page}) => {
  const pane = await openStory(page, 'home-home-view--logged-out');
  await expect(pane.getByRole('button', {name: 'Login to see projects'})).toBeVisible();
  await docsScreenshot(page, 'home-logged-out', pane);
});

test('home screen, logged in', async ({page}) => {
  const pane = await openStory(page, 'home-home-view--logged-in');
  await expect(pane.getByRole('button', {name: 'Download'}).first()).toBeVisible();
  await docsScreenshot(page, 'home-logged-in', pane);
});
});

test.describe('project', () => {
  test.beforeEach(async ({page}) => {
    await page.goto('/testing/project-view');
    await expect(page.locator('[role="table"]')).toBeVisible({timeout: 10_000});
  });

  test('a word open', async ({page}) => {
    // The list is virtualized: scroll until the row exists.
    const row = page.getByRole('row', {name: 'ambuka to cross a body of'});
    await page.locator('[role="table"]').hover();
    await expect(async () => {
      if (!await row.isVisible()) await page.mouse.wheel(0, 300);
      await expect(row).toBeVisible({timeout: 200});
    }).toPass();
    await row.click();
    await expect(page.getByRole('heading', {name: 'ambuka'})).toBeVisible();
    // The word's first field takes focus a moment after it opens.
    await blurFocusedField(page, page.locator('input:focus, [contenteditable="true"]:focus'));
    await page.mouse.move(0, 0);
    await docsScreenshot(page, 'project-word-open');
  });

  test('view options with the View chooser', async ({page}) => {
    const button = page.locator('button:has(.i-mdi-layers)').first();
    await button.click();
    const popover = page.locator('[data-slot="popover-content"]');
    await expect(popover.getByRole('radio', {name: 'FieldWorks Classic'})).toBeVisible();
    await expect(popover).toBeVisible();
    await docsScreenshot(page, 'view-options', [button, popover]);
  });
});
