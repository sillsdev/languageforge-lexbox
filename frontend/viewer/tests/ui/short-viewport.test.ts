import {expect, test, type Page} from '@playwright/test';
import {DemoProjectPage} from './demo-project.page';

// Landscape phones leave ~150 CSS px above the keyboard, so the editor sheds its sticky chrome there
// and the add-sense FAB gets out of the way while a field is being typed in.

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

  test('the add-sense FAB hides while a field is being typed in and comes back on blur', async ({page}) => {
    const projectPage = await openEntry(page);
    const fab = page.getByRole('button', {name: /^(sense|meaning)$/i});
    await expect(fab).toBeVisible();

    await fab.tap();
    const senseCount = await projectPage.entryView.getSenseCount();
    await expect(await projectPage.entryView.getGlossInput(senseCount - 1)).toBeFocused();
    await expect(fab).toBeHidden();

    await page.evaluate(() => (document.activeElement as HTMLElement).blur());
    await expect(fab).toBeVisible();
  });
});
