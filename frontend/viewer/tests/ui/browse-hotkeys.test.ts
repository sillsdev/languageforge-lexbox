import {expect, test, type Locator, type Page} from '@playwright/test';

import {DemoProjectPage} from './demo-project.page';

/** Demo project uses dictionary terminology ("New Word"); entry view would say "New Entry". */
function newEntryDialog(page: Page): Locator {
  return page.getByRole('dialog').filter({has: page.getByRole('heading', {name: /New (Entry|Word)/})});
}

test.describe('Browse hotkeys', () => {
  let projectPage: DemoProjectPage;

  test.beforeEach(async ({page}) => {
    projectPage = new DemoProjectPage(page);
    await projectPage.goto();
  });

  test.describe('New entry (Ctrl/Cmd+E)', () => {
    test('Ctrl/Cmd+E opens the new entry dialog', async ({page}) => {
      await page.keyboard.press('ControlOrMeta+e');
      await expect(newEntryDialog(page)).toBeVisible();
    });

    test('Ctrl/Cmd+E works while the search input is focused', async ({page}) => {
      await projectPage.entriesList.searchInput.click();
      await expect(projectPage.entriesList.searchInput).toBeFocused();

      await page.keyboard.press('ControlOrMeta+e');
      await expect(newEntryDialog(page)).toBeVisible();
    });

    test('plain E does not open the new entry dialog', async ({page}) => {
      await page.keyboard.press('e');
      await expect(page.getByRole('dialog')).toHaveCount(0);
    });

    test('Ctrl/Cmd+E does not reset an already-open new entry dialog', async ({page}) => {
      await page.keyboard.press('ControlOrMeta+e');

      const dialog = newEntryDialog(page);
      await expect(dialog).toBeVisible();

      const lexemeInput = dialog.locator('[style*="grid-area: lexemeForm"] input').first();
      await expect(lexemeInput).toBeVisible();
      await lexemeInput.fill('hotkey-preserve');
      await expect(lexemeInput).toHaveValue('hotkey-preserve');

      await page.keyboard.press('ControlOrMeta+e');

      await expect(page.getByRole('dialog')).toHaveCount(1);
      await expect(lexemeInput).toHaveValue('hotkey-preserve');
    });

    test('Ctrl/Cmd+E does nothing when the project is read-only', async ({page}) => {
      await page.evaluate(async () => {
        await window.__PLAYWRIGHT_UTILS__.setWrite(false);
      });
      await expect(page.getByRole('button', {name: /New (Entry|Word)/})).toHaveCount(0);

      await page.keyboard.press('ControlOrMeta+e');
      await expect(page.getByRole('dialog')).toHaveCount(0);
    });
  });

  test.describe('Search focus (Ctrl/Cmd+F)', () => {
    test('Ctrl/Cmd+F focuses the Filter search input and selects existing text', async ({page}) => {
      const filter = 'hotkey-select';
      await projectPage.entriesList.searchInput.fill(filter);
      await projectPage.entriesList.searchInput.blur();
      await expect(projectPage.entriesList.searchInput).not.toBeFocused();

      await page.keyboard.press('ControlOrMeta+f');

      const searchInput = projectPage.entriesList.searchInput;
      await expect(searchInput).toBeFocused();
      await expect(searchInput).toHaveValue(filter);
      await expect.poll(async () => searchInput.evaluate((el: HTMLInputElement) => ({
        start: el.selectionStart,
        end: el.selectionEnd,
        length: el.value.length,
      }))).toEqual({start: 0, end: filter.length, length: filter.length});
    });

    test('Ctrl/Cmd+F focuses search after selecting an entry', async ({page}) => {
      await projectPage.entriesList.selectEntryByIndex(0);
      await expect(projectPage.entriesList.searchInput).not.toBeFocused();

      await page.keyboard.press('ControlOrMeta+f');
      await expect(projectPage.entriesList.searchInput).toBeFocused();
    });
  });
});
