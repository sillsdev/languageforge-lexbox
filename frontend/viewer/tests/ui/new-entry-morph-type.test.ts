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

function lexemeInput(dialog: Locator, index = 0): Locator {
  return dialog.locator('[data-field-id="lexemeForm"]').locator('input').nth(index);
}

function morphTypeSelect(scope: Locator | Page): Locator {
  return scope.locator('[data-field-id="morphType"]:visible').getByRole('combobox');
}

async function createdMorphTypes(page: Page, lexeme: string): Promise<string[]> {
  return page.evaluate(async (lexeme) => {
    const entries = await window.__PLAYWRIGHT_UTILS__.demoApi.getEntries(undefined);
    return entries.filter(e => Object.values(e.lexemeForm).includes(lexeme)).map(e => e.morphType);
  }, lexeme);
}

test.describe('New entry morph type', () => {
  test('classic view picks the morph type from affix markers and stores the bare form', async ({page}) => {
    await new DemoProjectPage(page).goto();
    await useView(page, /FieldWorks Classic/);

    const dialog = await openNewEntryDialog(page);
    await expect(morphTypeSelect(dialog)).toHaveText('stem');
    await lexemeInput(dialog).fill('zed zed');
    await expect(morphTypeSelect(dialog)).toHaveText('phrase');
    await lexemeInput(dialog).fill('-zed');
    await expect(morphTypeSelect(dialog)).toHaveText('suffix');
    await lexemeInput(dialog).fill('zed-');
    await expect(morphTypeSelect(dialog)).toHaveText('prefix');

    await morphTypeSelect(dialog).click();
    await expect(page.getByText('Affixes', {exact: true})).toBeVisible();
    await page.getByRole('option', {name: 'infix', exact: true}).click();
    await expect(lexemeInput(dialog)).toHaveValue('-zed-');

    await dialog.getByRole('button', {name: /create entry/i}).click();
    await expect(dialog).toBeHidden();
    expect(await createdMorphTypes(page, 'zed')).toEqual(['Infix']);

    await test.step('the entry editor shows the morph type read-only', async () => {
      await expect(morphTypeSelect(page)).toHaveText('infix');
      await expect(morphTypeSelect(page)).toBeDisabled();
    });
  });

  test('classic view keeps a picked type the markers allow and blocks forms that fit no type', async ({page}) => {
    await new DemoProjectPage(page).goto();
    await useView(page, /FieldWorks Classic/);

    const dialog = await openNewEntryDialog(page);
    await morphTypeSelect(dialog).click();
    await page.getByRole('option', {name: 'root', exact: true}).click();
    await lexemeInput(dialog).fill('zed');
    await expect(morphTypeSelect(dialog)).toHaveText('root');

    await lexemeInput(dialog).fill('*zed-');
    await expect(dialog.getByText(`Check the markers in "*zed-". Did you mean "*zed", "zed-", or "-zed-"?`)).toBeVisible();
    await expect(lexemeInput(dialog)).toHaveAttribute('aria-invalid', 'true');

    await test.step('an error stays put while the form is edited into another error', async () => {
      await lexemeInput(dialog).fill('--zed~');
      await expect(dialog.getByText(`Check the markers in "--zed~".`, {exact: false})).toBeVisible();
      await lexemeInput(dialog).press('Home');
      await lexemeInput(dialog).press('Delete');
      // shorter than the pause new notes wait for
      await expect(lexemeInput(dialog)).toHaveAttribute('aria-invalid', 'true', {timeout: 300});
      await expect(dialog.getByText(`Check the markers in "-zed~".`, {exact: false})).toBeVisible();
    });

    await lexemeInput(dialog).fill('--zed');
    await expect(morphTypeSelect(dialog)).toHaveText('suffix');
    await expect(dialog.getByText(`"--zed" will be saved as "-zed". Is there an extra "-"?`)).toBeVisible();

    await lexemeInput(dialog).fill('-zed');
    await lexemeInput(dialog, 1).fill('zed');
    await dialog.getByRole('button', {name: /create entry/i}).click();
    await expect(dialog.getByText(`For morph type suffix, type "-zed".`)).toBeVisible();
    await expect(lexemeInput(dialog, 1)).toBeFocused();

    await lexemeInput(dialog, 1).fill('');
    await morphTypeSelect(dialog).click();
    await page.getByRole('option', {name: 'circumfix', exact: true}).click();
    await lexemeInput(dialog).fill('');
    await lexemeInput(dialog).pressSequentially('a- -b');
    await expect(morphTypeSelect(dialog)).toHaveText('circumfix');
  });

  test('lite view shows the morph type picker only once an affix is typed', async ({page}) => {
    await new DemoProjectPage(page).goto();
    await useView(page, /FieldWorks Lite/);

    const dialog = await openNewEntryDialog(page);
    await lexemeInput(dialog).fill('well-known');
    await lexemeInput(dialog).fill('zed zed');
    await expect(morphTypeSelect(dialog)).toHaveCount(0);
    await lexemeInput(dialog).fill('-zed');
    await expect(morphTypeSelect(dialog)).toHaveText('suffix');
    await lexemeInput(dialog).fill('zed');
    await expect(morphTypeSelect(dialog)).toHaveText('stem');
    await lexemeInput(dialog).fill('a- -b');
    await expect(morphTypeSelect(dialog)).toHaveText('circumfix');
    await lexemeInput(dialog).fill('a- b');
    await expect(morphTypeSelect(dialog)).toHaveText('phrase');
    await lexemeInput(dialog).fill('-zed');

    await dialog.getByRole('button', {name: /add word/i}).click();
    await expect(dialog).toBeHidden();
    expect(await createdMorphTypes(page, 'zed')).toEqual(['Suffix']);
  });
});
