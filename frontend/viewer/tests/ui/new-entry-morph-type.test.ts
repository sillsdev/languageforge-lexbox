import {expect, test, type Locator, type Page} from '@playwright/test';
import {DemoProjectPage} from './demo-project.page';

async function useView(page: Page, view: RegExp): Promise<void> {
  await page.locator('button:has(.i-mdi-layers)').first().click();
  await page.getByRole('radio', {name: view}).click();
  await page.keyboard.press('Escape');
}

async function openNewEntryDialog(page: Page): Promise<Locator> {
  await page.getByRole('button', {name: /new (entry|word)/i}).first().click();
  const dialog = page.getByRole('dialog');
  await expect(dialog).toBeVisible();
  return dialog;
}

function lexemeInput(dialog: Locator): Locator {
  return dialog.locator('[data-field-id="lexemeForm"]').locator('input').first();
}

function morphTypeSelect(dialog: Locator): Locator {
  return dialog.locator('[style*="grid-area: morphType"]').getByRole('combobox');
}

test.describe('New entry morph type', () => {
  test('classic view picks the morph type from affix markers and stores the bare form', async ({page}) => {
    const projectPage = new DemoProjectPage(page);
    await projectPage.goto();
    await useView(page, /FieldWorks Classic/);

    const dialog = await openNewEntryDialog(page);
    await expect(morphTypeSelect(dialog)).toHaveText(/stem/);

    await lexemeInput(dialog).fill('-zed');
    await expect(morphTypeSelect(dialog)).toHaveText(/suffix/);
    await lexemeInput(dialog).fill('zed-');
    await expect(morphTypeSelect(dialog)).toHaveText(/prefix/);

    await morphTypeSelect(dialog).click();
    await page.getByRole('option', {name: 'infix', exact: true}).click();
    await expect(lexemeInput(dialog)).toHaveValue('-zed-');

    await dialog.getByRole('button', {name: /create entry/i}).click();
    await expect(dialog).toBeHidden();
    const created = await page.evaluate(async () => {
      const api = window.__PLAYWRIGHT_UTILS__.demoApi;
      const entries = await api.getEntries(undefined);
      return entries.filter(e => Object.values(e.lexemeForm).includes('zed')).map(e => e.morphType);
    });
    expect(created).toEqual(['Infix']);
  });

  test('classic view rejects unknown marker combinations', async ({page}) => {
    const projectPage = new DemoProjectPage(page);
    await projectPage.goto();
    await useView(page, /FieldWorks Classic/);

    const dialog = await openNewEntryDialog(page);
    await lexemeInput(dialog).fill('*zed-');
    await dialog.getByRole('button', {name: /create entry/i}).click();
    await expect(dialog.getByText(/doesn't recognize the affix markers/)).toBeVisible();
  });

  test('lite view has no morph type picker and keeps the form as typed', async ({page}) => {
    const projectPage = new DemoProjectPage(page);
    await projectPage.goto();
    await useView(page, /FieldWorks Lite/);

    const dialog = await openNewEntryDialog(page);
    await expect(morphTypeSelect(dialog)).toHaveCount(0);
    await lexemeInput(dialog).fill('-zed');
    await dialog.getByRole('button', {name: /add word/i}).click();
    await expect(dialog).toBeHidden();
    const created = await page.evaluate(async () => {
      const api = window.__PLAYWRIGHT_UTILS__.demoApi;
      const entries = await api.getEntries(undefined);
      return entries.filter(e => Object.values(e.lexemeForm).includes('-zed')).map(e => e.morphType);
    });
    expect(created).toEqual(['Stem']);
  });
});
