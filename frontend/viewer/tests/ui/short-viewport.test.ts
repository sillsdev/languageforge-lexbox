import {expect, test, type Page} from '@playwright/test';
import {DemoProjectPage} from './demo-project.page';

// SHORT_BREAKPOINT: the viewport is this short when the keyboard is open or on a landscape phone.

async function openEntry(page: Page) {
  const projectPage = new DemoProjectPage(page);
  await projectPage.goto();
  await projectPage.tapEntry(3);
  return projectPage;
}

function senseHeader(page: Page) {
  return page.getByRole('heading', {name: /^(sense|meaning) 1$/i}).locator('..');
}

test.describe('Short viewport', () => {
  test.use({hasTouch: true, isMobile: true, viewport: {width: 1024, height: 400}});

  test('the sense header is not sticky', async ({page}) => {
    await openEntry(page);
    await expect(senseHeader(page)).toHaveCSS('position', 'static');

    await page.setViewportSize({width: 1024, height: 768});
    await expect(senseHeader(page)).toHaveCSS('position', 'sticky');
  });

  test('the entry header disappears when the keyboard leaves almost nothing', async ({page}) => {
    await openEntry(page);
    const entryHeader = page.locator('header', {has: page.getByRole('heading', {level: 2})}).first();
    await expect(entryHeader).toBeVisible();

    await page.setViewportSize({width: 1024, height: 200});
    await expect(entryHeader).toBeHidden();

    await page.setViewportSize({width: 1024, height: 400});
    await expect(entryHeader).toBeVisible();
  });
});

test.describe('Phone', () => {
  test.use({hasTouch: true, isMobile: true, viewport: {width: 412, height: 915}});

  test('the add-sense FAB gives way to the inline button when the keyboard shrinks the viewport', async ({page}) => {
    const projectPage = await openEntry(page);
    const fab = page.getByRole('button', {name: /^(sense|meaning)$/i});
    await expect(fab).toBeVisible();
    await expect(projectPage.entryView.addSenseButton).toBeHidden();

    await page.setViewportSize({width: 412, height: 530});
    await expect(fab).toBeHidden();
    await expect(projectPage.entryView.addSenseButton).toBeAttached();

    await page.setViewportSize({width: 412, height: 915});
    await expect(fab).toBeVisible();
  });
});
