import {expect, test, type Locator, type Page} from '@playwright/test';
import {BrowsePage} from './browse-page';

/**
 * Tests for the possible-duplicates check in the new entry dialog:
 * typing an existing word/gloss surfaces similar entries, a brand-new word
 * gets the "no similar entries" indicator, and a duplicate can be opened.
 */

// demo data (see demo-entry-data.ts): entry 'baba' exists, glossed 'father'
const existingLexeme = 'baba';
const existingGloss = 'father';

async function openNewEntryDialog(page: Page): Promise<Locator> {
  await page.getByRole('button', {name: /new (entry|word)/i}).first().click();
  const dialog = page.getByRole('dialog');
  await expect(dialog).toBeVisible();
  return dialog;
}

function lexemeInput(dialog: Locator): Locator {
  return dialog.locator('[style*="grid-area: lexemeForm"]').locator('input').first();
}

function glossInput(dialog: Locator): Locator {
  return dialog.locator('[style*="grid-area: gloss"]').locator('input').first();
}

const duplicatesSummary = /already exist/i;
const newWordIndicator = /no similar entries found|looks like a new word/i;

function duplicateRows(dialog: Locator): Locator {
  return dialog.getByRole('button', {name: /^go to (entry|word)/i});
}

test.describe('New entry possible duplicates', () => {
  test('typing an existing word shows duplicates and can navigate to one', async ({page}) => {
    const browsePage = new BrowsePage(page);
    await browsePage.goto();

    const dialog = await openNewEntryDialog(page);
    await lexemeInput(dialog).fill(existingLexeme);

    // exact headword match => attention strip + auto-expanded list
    await expect(dialog.getByText(duplicatesSummary)).toBeVisible();
    // 'baba' is a substring of 'ubaba', so both rows match — .first() is the exact match because same-word sorts first
    const duplicateRow = duplicateRows(dialog).filter({hasText: existingLexeme}).first();
    await expect(duplicateRow).toBeVisible();

    await duplicateRow.click();
    await expect(dialog).toBeHidden();
    await expect(page).toHaveURL(/entryId=/);
    const openedLexeme = await browsePage.entryView.getLexemeInput();
    await expect(openedLexeme).toHaveValue(existingLexeme);
  });

  test('brand-new word shows the new-word indicator', async ({page}) => {
    const browsePage = new BrowsePage(page);
    await browsePage.goto();

    const dialog = await openNewEntryDialog(page);
    await lexemeInput(dialog).fill('zyzzyvazz');
    await expect(dialog.getByText(newWordIndicator)).toBeVisible();
    await expect(dialog.getByText(duplicatesSummary)).toBeHidden();
  });

  test('typed meaning can be added to an existing entry instead', async ({page}) => {
    const browsePage = new BrowsePage(page);
    await browsePage.goto();

    const dialog = await openNewEntryDialog(page);
    await lexemeInput(dialog).fill(existingLexeme);
    const newGloss = `rescued-${Date.now().toString().slice(-6)}`;
    await glossInput(dialog).fill(newGloss);

    // exact match auto-expands the list
    const row = dialog.locator('li').filter({hasText: existingLexeme}).first();
    await row.getByRole('button', {name: /add (sense|meaning)/i}).click();

    await expect(dialog).toBeHidden();
    await expect(page).toHaveURL(/entryId=/);
    const entryId = new URL(page.url()).searchParams.get('entryId')!;
    await expect(async () => {
      expect(await browsePage.api.entryHasGlossValue(entryId, newGloss)).toBe(true);
    }).toPass({timeout: 5000});
  });

  test('partial headword match shows a collapsed strip with a similar-word badge', async ({page}) => {
    const browsePage = new BrowsePage(page);
    await browsePage.goto();

    const dialog = await openNewEntryDialog(page);
    // substring of 'balalika' only — no exact match, so the strip stays collapsed
    await lexemeInput(dialog).fill('balal');

    const summary = dialog.getByText(duplicatesSummary);
    await expect(summary).toBeVisible();
    await expect(duplicateRows(dialog)).toHaveCount(0);
    await summary.click();
    await expect(dialog.getByText(/similar (headword|word)/i).first()).toBeVisible();
  });

  test('long match lists collapse behind Show more and a capped count', async ({page}) => {
    const browsePage = new BrowsePage(page);
    await browsePage.goto();

    const dialog = await openNewEntryDialog(page);
    await lexemeInput(dialog).fill('ba');

    const summary = dialog.getByText(duplicatesSummary);
    await expect(summary).toBeVisible();
    await expect(dialog.getByText('10+')).toBeVisible();

    const rows = duplicateRows(dialog);
    if (await rows.count() === 0) await summary.click(); // expands automatically only on an exact match
    await expect(rows).toHaveCount(3);
    await dialog.getByRole('button', {name: /show \d+ more/i}).click();
    expect(await rows.count()).toBeGreaterThan(3);
  });

  test('matching gloss shows duplicates with a meaning badge', async ({page}) => {
    const browsePage = new BrowsePage(page);
    await browsePage.goto();

    const dialog = await openNewEntryDialog(page);
    await lexemeInput(dialog).fill('zyzzyvazz');
    await glossInput(dialog).fill(existingGloss);

    const summary = dialog.getByText(duplicatesSummary);
    await expect(summary).toBeVisible();
    // gloss-only matches don't auto-expand; expand to see the badge
    await summary.click();
    await expect(dialog.getByText(/similar (gloss|meaning)/i).first()).toBeVisible();
  });
});
