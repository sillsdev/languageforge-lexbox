import {expect, test, type Page} from '@playwright/test';
import {DemoProjectPage} from './demo-project.page';

// Opening an entry in the browse view focuses its first field, but only when the device has a hardware
// keyboard: without one that would pop up the virtual keyboard. Screen size and touch must not matter,
// so both tablet cases use a wide touch viewport and differ only in the reported keyboard.

function entryRow(page: Page, headword: string) {
  return page.locator('[role="row"]').filter({has: page.getByRole('heading', {name: headword, exact: true})});
}

test.describe('Entry autofocus', () => {
  test('desktop: clicking an entry focuses the lexeme field', async ({page}) => {
    const projectPage = new DemoProjectPage(page);
    await projectPage.goto();

    const {headword} = await projectPage.api.getEntryAtIndex(3);
    await entryRow(page, headword).click();

    await expect(await projectPage.entryView.getLexemeInput()).toBeFocused();
  });

  test.describe('touch tablet', () => {
    test.use({hasTouch: true, isMobile: true, viewport: {width: 1024, height: 768}});

    test('without a hardware keyboard, tapping an entry does not focus a field', async ({page}) => {
      const projectPage = new DemoProjectPage(page);
      await projectPage.goto();
      await page.evaluate(() => window.__PLAYWRIGHT_UTILS__.setHasHardwareKeyboard(false));

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

    test('with a hardware keyboard, tapping an entry focuses the lexeme field', async ({page}) => {
      const projectPage = new DemoProjectPage(page);
      await projectPage.goto();

      const {headword} = await projectPage.api.getEntryAtIndex(3);
      await entryRow(page, headword).tap();

      const lexemeInput = await projectPage.entryView.getLexemeInput();
      await expect(lexemeInput).toHaveValue(headword);
      await expect(lexemeInput).toBeFocused();
    });
  });
});
