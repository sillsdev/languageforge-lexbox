import {type Page, expect, test} from '@playwright/test';
import {SortField} from '$lib/dotnet-types/generated-types/MiniLcm/SortField';

/**
 * Critical tests: Verify that entry edits are saved to the backend.
 *
 * Note: The demo environment uses in-memory storage that resets on page reload.
 * We use existing demo entries and verify changes via API immediately after UI edits.
 * This catches the most common failure mode: UI appearing to save but not calling the API.
 */
test.describe('Entry edit persistence', () => {

  function filterLocator(page: Page) {
    return page.getByRole('textbox', {name: 'Filter'});
  }

  async function waitForProjectViewReady(page: Page) {
    await expect(page.locator('.i-mdi-loading')).toHaveCount(0, {timeout: 10000});
    await page.waitForFunction(() => document.fonts.ready);
    await expect(page.locator('[data-skeleton]')).toHaveCount(0, {timeout: 10000});
  }

  async function waitForTestUtils(page: Page) {
    await page.waitForFunction(() => window.__PLAYWRIGHT_UTILS__?.demoApi, {timeout: 5000});
  }

  async function waitForEntrySaved(page: Page) {
    await page.waitForTimeout(600);
    await expect(page.locator('.i-mdi-loading')).toHaveCount(0, {timeout: 5000});
  }

  async function selectEntryByFilter(page: Page, filter: string) {
    await filterLocator(page).fill(filter);
    await page.waitForTimeout(500);
    await expect(page.locator('[data-skeleton]')).toHaveCount(0, {timeout: 5000});

    const entryRow = page.getByRole('row', {name: new RegExp(filter.slice(0, 5))}).first();
    await expect(entryRow).toBeVisible({timeout: 10000});
    await entryRow.click();

    await expect(page.locator('.i-mdi-dots-vertical')).toBeVisible({timeout: 5000});
  }

  test.beforeEach(async ({page}) => {
    await page.goto('/testing/project-view');
    await waitForProjectViewReady(page);
    await waitForTestUtils(page);
  });

  test('UI edit of gloss field is saved to backend', async ({page}) => {
    // Get an existing entry with senses from demo data that has an English gloss
    const {entryId, headword, originalGloss} = await page.evaluate(async (headwordField) => {
      const api = window.__PLAYWRIGHT_UTILS__.demoApi;
      const entries = await api.getEntries({
        offset: 0,
        count: 50,
        order: {field: headwordField, writingSystem: 'default', ascending: true}
      });
      // Find an entry that has at least one sense with an English gloss
      const entry = entries.find(e => e.senses.length > 0 && e.senses[0].gloss?.en);
      if (!entry) throw new Error('No suitable entry with English gloss found in demo data');
      return {
        entryId: entry.id,
        headword: entry.citationForm?.seh ?? entry.lexemeForm?.seh ?? '',
        originalGloss: entry.senses[0].gloss?.en ?? '',
      };
    }, SortField.Headword);
    expect(entryId).toBeTruthy();
    expect(headword).toBeTruthy();
    expect(originalGloss).toBeTruthy();

    // Select the entry
    await selectEntryByFilter(page, headword);

    // Find the gloss field - look specifically for the English gloss input
    // The gloss field has multiple inputs (one per writing system)
    const glossFieldContainer = page.locator('[style*="grid-area: gloss"]').first();
    await expect(glossFieldContainer).toBeVisible({timeout: 5000});

    // Find the English input specifically (labeled "Eng" or similar)
    const engLabel = glossFieldContainer.locator('label:has-text("Eng")');
    if (await engLabel.count() > 0) {
      // Find the input associated with this label
      const labelFor = await engLabel.getAttribute('for');
      if (labelFor) {
        const glossInput = page.locator(`#${labelFor}`);
        await expect(glossInput).toBeVisible({timeout: 5000});

        // Verify we have the expected original value
        await expect(glossInput).toHaveValue(originalGloss);

        // Edit the gloss
        const timestamp = Date.now().toString().slice(-6);
        const newGloss = `edited-${timestamp}`;
        await glossInput.click();
        await glossInput.press('Control+a');
        await glossInput.fill(newGloss);
        await glossInput.press('Tab');
        await waitForEntrySaved(page);

        // Verify via API that the change was saved
        await expect(async () => {
          const savedEntry = await page.evaluate(async (id) => {
            const api = window.__PLAYWRIGHT_UTILS__.demoApi;
            return await api.getEntry(id);
          }, entryId);
          const savedGloss = savedEntry!.senses[0]?.gloss?.en ?? '';
          expect(savedGloss).toBe(newGloss);
        }).toPass({timeout: 5000});

        // Also verify the UI shows the new value
        await expect(glossInput).toHaveValue(newGloss);
        return;
      }
    }

    // Fallback: just use first input
    const glossInput = glossFieldContainer.locator('input').first();
    await expect(glossInput).toBeVisible({timeout: 5000});

    const timestamp = Date.now().toString().slice(-6);
    const newGloss = `edited-${timestamp}`;
    await glossInput.click();
    await glossInput.press('Control+a');
    await glossInput.fill(newGloss);
    await glossInput.press('Tab');
    await waitForEntrySaved(page);

    // Verify via API
    await expect(async () => {
      const savedEntry = await page.evaluate(async (id) => {
        const api = window.__PLAYWRIGHT_UTILS__.demoApi;
        return await api.getEntry(id);
      }, entryId);
      // Check all gloss values
      const allGlossValues = Object.values(savedEntry!.senses[0]?.gloss || {}).join(' ');
      expect(allGlossValues).toContain(newGloss);
    }).toPass({timeout: 5000});
  });

  test('adding sense via UI is saved to backend', async ({page}) => {
    // Get an existing entry and count its senses
    const {entryId, headword, initialSenseCount} = await page.evaluate(async (headwordField) => {
      const api = window.__PLAYWRIGHT_UTILS__.demoApi;
      const entries = await api.getEntries({
        offset: 15,
        count: 1,
        order: {field: headwordField, writingSystem: 'default', ascending: true}
      });
      const entry = entries[0];
      return {
        entryId: entry.id,
        headword: entry.citationForm?.seh ?? entry.lexemeForm?.seh ?? '',
        initialSenseCount: entry.senses.length,
      };
    }, SortField.Headword);
    expect(entryId).toBeTruthy();
    expect(headword).toBeTruthy();

    // Select the entry
    await selectEntryByFilter(page, headword);

    // Click Add Sense button
    const addSenseButton = page.getByRole('button', {name: /add (sense|meaning)/i});
    await expect(addSenseButton).toBeVisible({timeout: 5000});
    await addSenseButton.click();

    // Find the NEW sense's gloss input (it should be the last one)
    const glossFieldContainers = page.locator('[style*="grid-area: gloss"]');
    const count = await glossFieldContainers.count();
    expect(count).toBeGreaterThan(initialSenseCount);

    const newGlossContainer = glossFieldContainers.last();
    await expect(newGlossContainer).toBeVisible({timeout: 5000});

    const glossInput = newGlossContainer.locator('input').first();
    await expect(glossInput).toBeVisible({timeout: 5000});

    const timestamp = Date.now().toString().slice(-6);
    const senseGloss = `new-sense-${timestamp}`;
    await glossInput.fill(senseGloss);
    await glossInput.press('Tab');
    await waitForEntrySaved(page);

    // Verify via API that the sense was added
    await expect(async () => {
      const savedEntry = await page.evaluate(async (id) => {
        const api = window.__PLAYWRIGHT_UTILS__.demoApi;
        return await api.getEntry(id);
      }, entryId);
      expect(savedEntry!.senses.length).toBe(initialSenseCount + 1);
      const newSense = savedEntry!.senses.find(s =>
        Object.values(s.gloss || {}).some(v => v === senseGloss)
      );
      expect(newSense).toBeTruthy();
    }).toPass({timeout: 5000});
  });

  test('editing lexeme form is saved to backend', async ({page}) => {
    // Get an existing entry from the demo data
    const {entryId, originalLexeme} = await page.evaluate(async (headwordField) => {
      const api = window.__PLAYWRIGHT_UTILS__.demoApi;
      const entries = await api.getEntries({
        offset: 10,
        count: 1,
        order: {field: headwordField, writingSystem: 'default', ascending: true}
      });
      const entry = entries[0];
      return {
        entryId: entry.id,
        originalLexeme: entry.lexemeForm?.seh ?? '',
      };
    }, SortField.Headword);
    expect(entryId).toBeTruthy();
    expect(originalLexeme).toBeTruthy();

    // Search and select the entry
    await selectEntryByFilter(page, originalLexeme);

    // Find the lexeme form field
    const lexemeFieldContainer = page.locator('[style*="grid-area: lexemeForm"]');
    await expect(lexemeFieldContainer).toBeVisible({timeout: 5000});

    const lexemeInput = lexemeFieldContainer.locator('input').first();
    await expect(lexemeInput).toBeVisible({timeout: 5000});

    // Edit the lexeme
    const timestamp = Date.now().toString().slice(-6);
    const editMarker = `-E${timestamp}`;
    const newLexeme = originalLexeme + editMarker;

    await lexemeInput.click();
    await lexemeInput.press('Control+a');
    await lexemeInput.fill(newLexeme);
    await lexemeInput.press('Tab');
    await waitForEntrySaved(page);

    // Verify via API that the change was saved
    await expect(async () => {
      const savedEntry = await page.evaluate(async (id) => {
        const api = window.__PLAYWRIGHT_UTILS__.demoApi;
        return await api.getEntry(id);
      }, entryId);
      expect(savedEntry!.lexemeForm?.seh).toBe(newLexeme);
    }).toPass({timeout: 5000});

    // Also verify the UI shows the new value
    await expect(lexemeInput).toHaveValue(newLexeme);
  });
});
