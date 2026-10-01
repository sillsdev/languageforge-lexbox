import {expect, test, type Page} from '@playwright/test';
import {DemoProjectPage} from './demo-project.page';

// Opening an entry in the browse view focuses its first field, but only when that won't pop up a
// virtual keyboard: i.e. with a fine pointer (mouse/trackpad) or while the user is driving the app
// with a keyboard. Screen size must not matter, so the touch case uses a wide (tablet) viewport.
// Adding a sense or example is a request to type, so those focus the new field on every device.

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

    test('tapping Add sense focuses the new gloss field', async ({page}) => {
      const projectPage = new DemoProjectPage(page);
      await projectPage.goto();
      expect(await page.evaluate(() => matchMedia('(pointer: fine)').matches)).toBe(false);

      const {headword} = await projectPage.api.getEntryAtIndex(3);
      await entryRow(page, headword).tap();
      await expect(await projectPage.entryView.getLexemeInput()).toHaveValue(headword);
      const senseCount = await projectPage.entryView.getSenseCount();

      await projectPage.entryView.addSenseButton.tap();

      await expect(await projectPage.entryView.getGlossInput(senseCount)).toBeFocused();
    });

    async function focusNewExample(page: Page) {
      const projectPage = new DemoProjectPage(page);
      await projectPage.goto();
      const {headword} = await projectPage.api.getEntryAtIndex(3);
      await entryRow(page, headword).tap();
      await expect(await projectPage.entryView.getLexemeInput()).toHaveValue(headword);
      await page.getByRole('button', {name: /add example/i}).last().tap();
      await expect.poll(focusedField).toMatchObject({field: 'grid-area: sentence;'});

      function focusedField() {
        return page.evaluate(() => {
          const field = document.activeElement!;
          const {top, bottom} = field.getBoundingClientRect();
          return {field: field.closest('[style*="grid-area"]')?.getAttribute('style'), inView: top >= 0 && bottom <= window.innerHeight, top};
        });
      }
      return focusedField;
    }

    test('the focused field stays in view when the keyboard shrinks the viewport', async ({page}) => {
      const focusedField = await focusNewExample(page);
      // park the field near the bottom, where the keyboard would cover it
      await page.evaluate(() => document.activeElement!.scrollIntoView({block: 'end'}));
      await expect.poll(focusedField).toMatchObject({inView: true});

      // a virtual keyboard resizes the window well after focus; emulate that
      await page.setViewportSize({width: 1024, height: 300});

      await expect.poll(focusedField).toMatchObject({field: 'grid-area: sentence;', inView: true});
    });

    test('a focused field the user scrolled away from is left alone when the viewport shrinks', async ({page}) => {
      const focusedField = await focusNewExample(page);
      await page.evaluate(() => document.activeElement!.closest('[data-scroll-area-viewport], [class*="overflow"]')!.scrollBy(0, -2000));
      const scrolledAway = await focusedField();
      expect(scrolledAway.inView).toBe(false);

      await page.setViewportSize({width: 1024, height: 300});
      await page.waitForTimeout(500);

      // still well below the viewport, i.e. not re-centered (the resize itself may shift it a few px)
      const after = await focusedField();
      expect(after.inView).toBe(false);
      expect(after.top).toBeGreaterThan(scrolledAway.top - 100);
    });
  });
});
