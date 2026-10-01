import {expect, test, type Page} from '@playwright/test';
import {DemoProjectPage} from './demo-project.page';

// The keyboard shrinks the viewport (landscape phones: ~150 CSS px left, portrait: ~530), so below
// SHORT_BREAKPOINT the editor sheds its sticky chrome and swaps the add-sense FAB for the inline button.

function entryRow(page: Page, headword: string) {
  return page.locator('[role="row"]').filter({has: page.getByRole('heading', {name: headword, exact: true})});
}

async function openEntry(page: Page) {
  const projectPage = new DemoProjectPage(page);
  await projectPage.goto();
  const {headword} = await projectPage.api.getEntryAtIndex(3);
  await entryRow(page, headword).tap();
  await expect(await projectPage.entryView.getLexemeInput()).toHaveValue(headword);
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
