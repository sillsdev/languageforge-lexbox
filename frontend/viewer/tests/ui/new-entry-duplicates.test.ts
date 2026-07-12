import {expect, test, type Locator, type Page} from '@playwright/test';
import {DemoProjectPage} from './demo-project.page';

/**
 * Tests for the possible-duplicates check in the new entry dialog:
 * typing an existing word/gloss surfaces similar entries, a brand-new word
 * gets the "no similar entries" indicator, and a match row expands in place
 * to offer "Go to entry" / "Add sense".
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

/** The duplicate strip's header — .first() because the jump pill repeats the message when the strip is out of view. */
function stripSummary(dialog: Locator): Locator {
  return dialog.getByText(duplicatesSummary).first();
}

/** The visible match rows — each expands in place (aria-expanded) to reveal its actions; the collapsed strip keeps them in the DOM hidden. */
function duplicateRows(dialog: Locator): Locator {
  return dialog.locator('li > button[aria-expanded]:visible');
}

test.describe('New entry possible duplicates', () => {
  test('typing an existing word shows duplicates and can navigate to one', async ({page}) => {
    const projectPage = new DemoProjectPage(page);
    await projectPage.goto();

    const dialog = await openNewEntryDialog(page);
    await lexemeInput(dialog).fill(existingLexeme);

    // exact headword match => attention strip + auto-expanded list
    await expect(stripSummary(dialog)).toBeVisible();
    // 'baba' is a substring of 'ubaba', so both rows match — .first() is the exact match because same-word sorts first
    const duplicateRow = duplicateRows(dialog).filter({hasText: existingLexeme}).first();
    await duplicateRow.click();
    await expect(duplicateRow).toHaveAttribute('aria-expanded', 'true');

    await dialog.getByRole('button', {name: /go to (entry|word)/i}).click();
    await expect(dialog).toBeHidden();
    await expect(page).toHaveURL(/entryId=/);
    const openedLexeme = await projectPage.entryView.getLexemeInput();
    await expect(openedLexeme).toHaveValue(existingLexeme);
  });

  test('brand-new word shows the new-word indicator', async ({page}) => {
    const projectPage = new DemoProjectPage(page);
    await projectPage.goto();

    const dialog = await openNewEntryDialog(page);
    await lexemeInput(dialog).fill('zyzzyvazz');
    await expect(dialog.getByText(newWordIndicator)).toBeVisible();
    await expect(stripSummary(dialog)).toBeHidden();
  });

  test('typed meaning can be added to an existing entry instead', async ({page}) => {
    const projectPage = new DemoProjectPage(page);
    await projectPage.goto();

    const entryCountBefore = await projectPage.api.countEntries();
    const dialog = await openNewEntryDialog(page);
    await lexemeInput(dialog).fill(existingLexeme);
    const newGloss = `rescued-${Date.now().toString().slice(-6)}`;
    await glossInput(dialog).fill(newGloss);

    // exact match auto-expands the list; expand the row to reach its actions
    const duplicateRow = duplicateRows(dialog).filter({hasText: existingLexeme}).first();
    await duplicateRow.click();
    await dialog.getByRole('button', {name: /add (sense|meaning)/i}).click();

    await expect(dialog).toBeHidden();
    await expect(page).toHaveURL(/entryId=/);
    const entryId = new URL(page.url()).searchParams.get('entryId');
    expect(entryId).toBeTruthy();
    await expect(async () => {
      expect(await projectPage.api.entryHasGlossValue(entryId!, newGloss)).toBe(true);
    }).toPass({timeout: 5000});
    // the sense landed on the existing entry INSTEAD of a new one being created
    expect(await projectPage.api.countEntries()).toBe(entryCountBefore);
  });

  test('Enter inside the duplicate strip expands the row without creating the entry', async ({page}) => {
    const projectPage = new DemoProjectPage(page);
    await projectPage.goto();

    const entryCountBefore = await projectPage.api.countEntries();
    const dialog = await openNewEntryDialog(page);
    await lexemeInput(dialog).fill(existingLexeme);

    const duplicateRow = duplicateRows(dialog).filter({hasText: existingLexeme}).first();
    await duplicateRow.focus();
    await page.keyboard.press('Enter');

    // Enter activated the focused row, and was NOT also swallowed by the dialog's
    // submit-on-Enter handler — which would have created the very duplicate being warned about
    await expect(duplicateRow).toHaveAttribute('aria-expanded', 'true');
    await expect(dialog).toBeVisible();
    expect(await projectPage.api.countEntries()).toBe(entryCountBefore);
  });

  test('partial headword match shows a collapsed strip with a similar-word badge', async ({page}) => {
    const projectPage = new DemoProjectPage(page);
    await projectPage.goto();

    const dialog = await openNewEntryDialog(page);
    // substring of 'balalika' only — no exact match, so the strip stays collapsed
    await lexemeInput(dialog).fill('balal');

    const summary = stripSummary(dialog);
    await expect(summary).toBeVisible();
    await expect(duplicateRows(dialog)).toHaveCount(0);
    await summary.click();
    await expect(dialog.getByText(/similar (headword|word)/i).first()).toBeVisible();
  });

  test('long match lists collapse behind Show more and a capped count', async ({page}) => {
    const projectPage = new DemoProjectPage(page);
    await projectPage.goto();

    const dialog = await openNewEntryDialog(page);
    await lexemeInput(dialog).fill('ba');

    const summary = stripSummary(dialog);
    await expect(summary).toBeVisible();
    // dozens of demo lexemes contain 'ba', so the fetch cap is hit and the count renders as 'N+'
    await expect(dialog.getByText(/^\d+\+$/).first()).toBeVisible();

    const rows = duplicateRows(dialog);
    if (await rows.count() === 0) await summary.click(); // expands automatically only on an exact match
    // 3 = the component's initial display count
    await expect(rows).toHaveCount(3);
    await dialog.getByRole('button', {name: /show \d+ more/i}).click();
    expect(await rows.count()).toBeGreaterThan(3);
  });

  test('matching gloss shows duplicates with a meaning badge', async ({page}) => {
    const projectPage = new DemoProjectPage(page);
    await projectPage.goto();

    const dialog = await openNewEntryDialog(page);
    await lexemeInput(dialog).fill('zyzzyvazz');
    await glossInput(dialog).fill(existingGloss);

    const summary = stripSummary(dialog);
    await expect(summary).toBeVisible();
    // gloss-only matches don't auto-expand; expand to see the badge
    await summary.click();
    await expect(dialog.getByText(/similar (gloss|meaning)/i).first()).toBeVisible();
  });

  test('an out-of-view duplicate strip surfaces a jump pill', async ({page}) => {
    // small viewport so the duplicate strip (below the editor grid) starts outside the dialog's scroll view
    await page.setViewportSize({width: 1024, height: 560});
    const projectPage = new DemoProjectPage(page);
    await projectPage.goto();

    const dialog = await openNewEntryDialog(page);
    await lexemeInput(dialog).fill(existingLexeme);

    // the pill's accessible name is exactly the summary message; the strip trigger's name has more text
    const pill = dialog.getByRole('button', {name: /^this (entry|word) may already exist$/i});
    await expect(pill).toBeVisible();
    await pill.click();

    // jumping scrolls the strip into view, which dismisses the pill and shows the match rows
    await expect(duplicateRows(dialog).first()).toBeInViewport();
    await expect(pill).toBeHidden();
  });

  test('the jump pill can be dismissed and stays dismissed', async ({page}) => {
    await page.setViewportSize({width: 1024, height: 560});
    const projectPage = new DemoProjectPage(page);
    await projectPage.goto();

    const dialog = await openNewEntryDialog(page);
    await lexemeInput(dialog).fill(existingLexeme);

    const pill = dialog.getByRole('button', {name: /^this (entry|word) may already exist$/i});
    await expect(pill).toBeVisible();
    // the pill's Close is its sibling; dialogs have their own Close button, so scope to the pill's parent
    await pill.locator('..').getByRole('button', {name: /close/i}).click();
    await expect(pill).toBeHidden();

    // still dismissed while matches keep changing out of view
    await lexemeInput(dialog).fill(existingLexeme.slice(0, -1));
    await lexemeInput(dialog).fill(existingLexeme);
    await expect(dialog.getByText(duplicatesSummary).first()).toBeAttached();
    await expect(pill).toBeHidden();
  });
});
