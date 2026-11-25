import {expect, test, type Page} from '@playwright/test';

import {DemoProjectPage} from './demo-project.page';
import {MorphTypeKind} from '$lib/dotnet-types/generated-types/MiniLcm/Models/MorphTypeKind';

// The multi-select saves its toggles once, when it closes.
test.describe('Multi-select save', () => {
  let projectPage: DemoProjectPage;

  test.beforeEach(async ({page}) => {
    projectPage = new DemoProjectPage(page);
    await projectPage.goto();
  });

  async function createEntryWithSense(page: Page, headword: string): Promise<string> {
    return page.evaluate(async ({hw, morphType}) => {
      const id = crypto.randomUUID();
      await window.__PLAYWRIGHT_UTILS__.demoApi.createEntry({
        id,
        lexemeForm: {seh: hw},
        citationForm: {},
        senses: [{id: crypto.randomUUID(), entryId: id, gloss: {en: 'gloss'}, definition: {}, semanticDomains: [], exampleSentences: [], pictures: []}],
        note: {},
        literalMeaning: {},
        morphType,
        components: [],
        complexForms: [],
        complexFormTypes: [],
        publishIn: [],
        homographNumber: 0,
      }, {includeComplexFormsAndComponents: true, autoAddMainPublication: false});
      return id;
    }, {hw: headword, morphType: MorphTypeKind.Stem});
  }

  function senseDomainNames(page: Page, entryId: string): Promise<string[]> {
    return page.evaluate(async (id) => {
      const entry = await window.__PLAYWRIGHT_UTILS__.demoApi.getEntry(id);
      return entry?.senses[0].semanticDomains.map((d) => d.name['en'] ?? '') ?? [];
    }, entryId);
  }

  async function openDomainPicker(page: Page, headword: string) {
    await projectPage.entriesList.filterByText(headword);
    await projectPage.entriesList.entryRows.filter({hasNotText: 'Add to dictionary'}).filter({hasText: headword}).click();
    await page.locator('[data-field-id="semanticDomains"]').getByRole('combobox').click();
    await expect(page.getByRole('option', {name: 'Fruit'})).toBeVisible();
  }

  test('closing without toggling keeps a change made elsewhere meanwhile', async ({page}) => {
    const entryId = await createEntryWithSense(page, 'qmsremote');
    await openDomainPicker(page, 'qmsremote');

    await page.evaluate(async (id) => {
      const api = window.__PLAYWRIGHT_UTILS__.demoApi;
      const before = (await api.getEntry(id))!;
      const animal = (await api.getSemanticDomains()).find((d) => d.name['en'] === 'Animal')!;
      const after = structuredClone(before);
      after.senses[0].semanticDomains = [animal];
      await api.updateEntry(before, after);
    }, entryId);
    await expect.poll(() => senseDomainNames(page, entryId)).toEqual(['Animal']);

    await page.keyboard.press('Escape');
    await expect(page.getByRole('option', {name: 'Fruit'})).toBeHidden();
    await expect.poll(() => senseDomainNames(page, entryId)).toEqual(['Animal']);
  });

  test('a toggle and a change made elsewhere meanwhile both survive closing', async ({page}) => {
    const entryId = await createEntryWithSense(page, 'qmsmerge');
    await openDomainPicker(page, 'qmsmerge');
    await page.getByRole('option', {name: 'Fruit'}).click();

    await page.evaluate(async (id) => {
      const api = window.__PLAYWRIGHT_UTILS__.demoApi;
      const before = (await api.getEntry(id))!;
      const animal = (await api.getSemanticDomains()).find((d) => d.name['en'] === 'Animal')!;
      const after = structuredClone(before);
      after.senses[0].semanticDomains = [animal];
      await api.updateEntry(before, after);
    }, entryId);
    await expect.poll(() => senseDomainNames(page, entryId)).toEqual(['Animal']);

    await page.keyboard.press('Escape');
    await expect.poll(async () => (await senseDomainNames(page, entryId)).sort()).toEqual(['Animal', 'Fruit']);
  });

  test('a toggle reaches a new meaning when Add Meaning is clicked with the list open', async ({page}) => {
    const entryId = await createEntryWithSense(page, 'qmsaddsense');
    await projectPage.entriesList.filterByText('qmsaddsense');
    await projectPage.entriesList.entryRows.filter({hasNotText: 'Add to dictionary'}).filter({hasText: 'qmsaddsense'}).click();
    await page.getByRole('button', {name: 'Add Meaning'}).first().click();
    const dialog = page.getByRole('dialog');
    await dialog.locator('[data-field-id="gloss"] input').first().fill('second');
    await dialog.locator('[data-field-id="semanticDomains"]').getByRole('combobox').click();
    await page.getByRole('option', {name: 'Fruit'}).click();

    // One click both closes the list and submits the dialog.
    await dialog.getByRole('button', {name: 'Add Meaning'}).click();

    await expect(dialog).toBeHidden();
    await expect.poll(() => page.evaluate(async (id) => {
      const entry = await window.__PLAYWRIGHT_UTILS__.demoApi.getEntry(id);
      return entry?.senses.map((s) => s.semanticDomains.map((d) => d.name['en']));
    }, entryId)).toEqual([[], ['Fruit']]);
  });

  test('a toggle is saved when the editor goes away while the list is open', async ({page}) => {
    const entryId = await createEntryWithSense(page, 'qmsunmount');
    await openDomainPicker(page, 'qmsunmount');
    await page.getByRole('option', {name: 'Fruit'}).click();
    await expect(page.getByRole('option', {name: 'Fruit'})).toHaveAttribute('aria-checked', 'true');

    await page.evaluate(() => history.back());

    await expect.poll(() => senseDomainNames(page, entryId)).toEqual(['Fruit']);
  });
});
