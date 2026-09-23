import {expect, test, type Page} from '@playwright/test';
import {DemoProjectPage} from './demo-project.page';

// Opening an entry in the browse view focuses its first field, but only when that won't pop up a
// virtual keyboard: i.e. with a fine pointer (mouse/trackpad) or while the user is driving the app
// with a keyboard. Screen size must not matter, so the touch case uses a wide (tablet) viewport.

function entryRow(page: Page, headword: string) {
  return page.locator('[role="row"]').filter({has: page.getByRole('heading', {name: headword, exact: true})});
}

test.describe('Entry autofocus', () => {
  test('desktop: clicking an entry focuses the lexeme field', async ({page}) => {
    const projectPage = new DemoProjectPage(page);
    await projectPage.goto();
    expect(await page.evaluate(() => matchMedia('(pointer: fine)').matches)).toBe(true);

    const {headword} = await projectPage.api.getEntryAtIndex(3);
    await entryRow(page, headword).click();

    await expect(await projectPage.entryView.getLexemeInput()).toBeFocused();
  });

  test.describe('touch tablet', () => {
    test.use({hasTouch: true, isMobile: true, viewport: {width: 1024, height: 768}});

    test('tapping an entry does not focus a field', async ({page}) => {
      const projectPage = new DemoProjectPage(page);
      await projectPage.goto();
      // guard against the test passing vacuously: this must be a touch-first, desktop-width layout
      expect(await page.evaluate(() => matchMedia('(pointer: fine)').matches)).toBe(false);

      const first = await projectPage.api.getEntryAtIndex(3);
      await entryRow(page, first.headword).tap();
      const lexemeInput = await projectPage.entryView.getLexemeInput();
      await expect(lexemeInput).toHaveValue(first.headword);
      await expect(lexemeInput).not.toBeFocused();

      // and not when switching to another entry either
      const second = await projectPage.api.getEntryAtIndex(4);
      await entryRow(page, second.headword).tap();
      await expect(lexemeInput).toHaveValue(second.headword);
      await expect(lexemeInput).not.toBeFocused();
    });

    test('opening an entry with a keyboard focuses the lexeme field', async ({page}) => {
      const projectPage = new DemoProjectPage(page);
      await projectPage.goto();
      expect(await page.evaluate(() => matchMedia('(pointer: fine)').matches)).toBe(false);

      const {headword} = await projectPage.api.getEntryAtIndex(3);
      const row = entryRow(page, headword);
      await row.focus();
      await row.press('Enter');

      const lexemeInput = await projectPage.entryView.getLexemeInput();
      await expect(lexemeInput).toHaveValue(headword);
      await expect(lexemeInput).toBeFocused();
    });
  });
});
