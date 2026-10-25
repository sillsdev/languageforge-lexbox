import {expect, test} from '@playwright/test';
import {docsScreenshot, openStory} from './docs-screenshot';

// Whole-app shots for docs/fw-lite/getting-started.md, taken from the in-browser demo project.
// Names are what the doc pages reference (/img/fw-lite/<name>.png); renaming one breaks the docs.

test.describe('project view', () => {
  test.beforeEach(async ({page}) => {
    await page.goto('/testing/project-view');
    await expect(page.locator('[role="table"]')).toBeVisible({timeout: 10_000});
    await expect(page.getByRole('textbox', {name: 'Filter'})).toBeVisible();
  });

  test('entry list', async ({page}) => {
    await page.getByRole('row', {name: 'ambuka to cross a body of'}).scrollIntoViewIfNeeded();
    await docsScreenshot(page, 'project-view');
  });

  test('entry selected', async ({page}) => {
    await page.getByRole('row', {name: 'ambuka to cross a body of'}).click();
    await expect(page.getByRole('heading', {name: 'ambuka'})).toBeVisible();
    await expect(page.locator('.i-mdi-dots-vertical')).toBeVisible();
    await docsScreenshot(page, 'project-view-entry');
  });

  test('entry selected (mobile)', async ({page}) => {
    await page.setViewportSize({width: 375, height: 812});
    await page.getByRole('row', {name: 'ambuka to cross a body of'}).click();
    await expect(page.getByRole('heading', {name: 'ambuka'})).toBeVisible();
    await docsScreenshot(page, 'project-view-entry-mobile');
  });
});

// Component shots from Storybook: the stories already carry realistic fake data, so the home
// screen's server/project list can be captured without a backend or a login.
test.describe('components', () => {
  test('server project list', async ({page}) => {
    const pane = await openStory(page, 'home-server--server');
    // The story renders a loaded server and then a loading one; the docs want the loaded one.
    await docsScreenshot(page, 'home-server-projects', pane.locator('> *').first());
  });

  test('multi writing system input', async ({page}) => {
    const pane = await openStory(page, 'editor-fields-multi-ws-input--raw');
    await docsScreenshot(page, 'editor-multi-ws-input', pane);
  });
});

test.describe('project view details', () => {
  test.beforeEach(async ({page}) => {
    await page.goto('/testing/project-view');
    await expect(page.locator('[role="table"]')).toBeVisible({timeout: 10_000});
    await expect(page.getByRole('textbox', {name: 'Filter'})).toBeVisible();
  });

  test('view options with the View chooser', async ({page}) => {
    await page.locator('button:has(.i-mdi-layers)').first().click();
    await expect(page.getByRole('radio', {name: 'FieldWorks Classic'})).toBeVisible();
    await docsScreenshot(page, 'view-options');
  });

  test('Help & More with Feedback & Support', async ({page}) => {
    await page.getByRole('button', {name: 'Help & More'}).click();
    await expect(page.getByRole('button', {name: 'Feedback & Support'})).toBeVisible();
    await docsScreenshot(page, 'help-and-more');
  });

  test('read-only project', async ({page}) => {
    await page.evaluate(() => window.__PLAYWRIGHT_UTILS__.setWrite(false));
    await expect(page.getByLabel('Read-only')).toBeVisible();
    await page.getByRole('row', {name: 'ambuka to cross a body of'}).click();
    await expect(page.getByRole('heading', {name: 'ambuka'})).toBeVisible();
    await docsScreenshot(page, 'project-view-read-only');
  });
});
