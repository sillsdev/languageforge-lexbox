import {expect, test, type Locator, type Page} from '@playwright/test';

import {DemoProjectPage} from './demo-project.page';

/** Demo project uses dictionary terminology ("New Word"); entry view would say "New Entry". */
function newEntryDialog(page: Page): Locator {
  return page.getByRole('dialog').filter({has: page.getByRole('heading', {name: /New (Entry|Word)/})});
}

function newEntryButton(page: Page): Locator {
  return page.getByRole('button', {name: /New (Entry|Word)/});
}

/** bits-ui keeps closed tooltip content mounted, so match on state rather than presence. */
function openTooltip(page: Page): Locator {
  return page.locator('[data-tooltip-content]:not([data-state="closed"])');
}

async function tabTo(page: Page, target: Locator): Promise<void> {
  for (let i = 0; i < 25; i++) {
    await page.keyboard.press('Tab');
    if (await target.evaluate(el => el === document.activeElement)) return;
  }
  throw new Error('Tab never reached the target');
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

      const lexemeInput = dialog.locator('[data-field-id="lexemeForm"] input').first();
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

  test.describe('New entry shortcut tooltip', () => {
    test('shows on hover and on keyboard focus, with the shortcut', async ({page}) => {
      const button = newEntryButton(page);
      await button.hover();
      await expect(openTooltip(page)).toBeVisible();
      await expect(openTooltip(page)).toContainText(/Ctrl\+E|⌘E/);

      await page.mouse.move(600, 400);
      await expect(openTooltip(page)).toHaveCount(0);

      await tabTo(page, button);
      await expect(openTooltip(page)).toBeVisible();
    });

    test('does not show when closing the dialog hands focus back to the button', async ({page}) => {
      const button = newEntryButton(page);
      await button.click();
      const dialog = newEntryDialog(page);
      await expect(dialog).toBeVisible();

      await dialog.getByRole('button', {name: 'Close'}).click();
      await expect(dialog).toBeHidden();
      await expect(button).toBeFocused();
      await page.waitForTimeout(500);
      await expect(openTooltip(page)).toHaveCount(0);
    });

    test('never shows without a hardware keyboard', async ({page}) => {
      await page.evaluate(async () => {
        window.__PLAYWRIGHT_UTILS__.setHasHardwareKeyboard(false);
        // the tooltip reads the flag when the button mounts; toggling write re-mounts it
        await window.__PLAYWRIGHT_UTILS__.setWrite(false);
        await window.__PLAYWRIGHT_UTILS__.setWrite(true);
      });
      const button = newEntryButton(page);
      await expect(button).toBeVisible();

      await tabTo(page, button);
      await page.waitForTimeout(500);
      await expect(openTooltip(page)).toHaveCount(0);

      await button.hover();
      await page.waitForTimeout(500);
      await expect(openTooltip(page)).toHaveCount(0);

      await button.click();
      const dialog = newEntryDialog(page);
      await expect(dialog).toBeVisible();
      await dialog.getByRole('button', {name: 'Close'}).click();
      await expect(dialog).toBeHidden();
      await page.waitForTimeout(500);
      await expect(openTooltip(page)).toHaveCount(0);
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
